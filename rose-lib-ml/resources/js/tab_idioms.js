$(document).ready(function () {
    // filtering idioms by root node type
    $("form#idiomsForm").submit(filterIdiomsByRootType());

    // idiom setup form
    $("div#idiomList").on("click", "button.choose-idiom", openIdiomSetup());

    $("form#idiomSetupForm").submit(saveIdiomSetup());
    $("button#idiomSetupCancel").click(closeIdiomSetup());

    $("div#output-snippets").on("click", "button.removeSnippet", removeOutputSnippetFromState());

    // generate
    $("button#generateButton").click(generate());

    // restoring the state of the panel (after changing tabs)
    const previousState = vscode.getState();
    if(previousState !== undefined) {
        if(previousState.idioms !== undefined) {
            showIdioms(previousState.idioms);
        }

        if(previousState.outputSnippets !== undefined) {
            showOutputSnippets(previousState.outputSnippets);
        }
    }
    else {
        vscode.postMessage({
            command: "getIdioms",
            parameters: {
                rootNodeType: null
            }
        });
    }
});

$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.value !== null){
        switch(message.command) {
            case 'showIdioms':
                // save state - for panel restoring
                saveIdiomsToState(message.value);
                
                showIdioms(message.value);
                break;
            case 'showSnippetPreview':
                showOutputSnippetPreview(message.value, message.index);
                break;
            case 'clearOutputSnippets':
                clearOutputSnippets();
                break;
        }
    }
});

const vscode = acquireVsCodeApi();

/**
 * Sends a message to the extension in order to get idioms filtered by a root node type from LSP
 */
function filterIdiomsByRootType() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "getIdioms",
            parameters: {
                rootNodeType: $("#rootNodeType").val()
            }
        });
    };
}

/**
 * Show idioms in the preview
 * @param idioms 
 */
function showIdioms(idioms) {
    $("#idiomList").empty();
        
    for (let [i, idiom] of idioms.entries()) {
        let idiomHTML = `<div class="row mb-2">
                            <div class="col-10 mx-0 align-self-center fragment-snippet"><p>${idiom.fragment}</p></div>
                            <div class="col-2 align-self-center">
                                <button id="${i}" type="button" class="btn btn-lg btn-default vscbtn-sm choose-idiom">
                                    <i class="fas fa-chevron-right"></i>
                                </button>
                            </div>
                        </div>`;

        $("#idiomList").append(idiomHTML);
    } 
}

/**
 * Shows output snippets in the preview
 * @param outputSnippets 
 */
function showOutputSnippets(outputSnippets) {
    $("#output-snippets").empty();

    for (let i = 0; i < outputSnippets.length; i++){
        let snippet = outputSnippets[i];

        if(!snippet.removed){
            showOutputSnippet(snippet, i);
        }
    }
}

/**
 * Hides the list of idioms and shows the form for the setup of the chosen idiom
 */
function openIdiomSetup() {
    return function () {
        let currentState = vscode.getState();
        
        let index = $(this).attr('id');
        if(index === undefined) {
            return;
        }

        if (currentState !== undefined && currentState.idioms !== undefined) {
            let chosenIdiom = currentState.idioms[index];

            if (chosenIdiom === undefined) {
                return;
            }

            // add input fields for metavariables
            $("div#idiomMetavariables").empty();

            for (let i = 0; i < chosenIdiom.metavariables.length; i++) {
                let title = `Metavariable ${i+1}: ${chosenIdiom.metavariables[i]}`;
                
                let formField = `<div class="form-group mb-4">
                                    <label for="metavariable${i}">${title}</label>
                                    <input data-metavariable="${chosenIdiom.metavariables[i]}" id="metavariable${i}" class="form-control metavariable" type="text" required>
                                </div>`;
                
                $("div#idiomMetavariables").append(formField);
            }

            // add options with available composers
            $('#composer').empty();

            for (let composer of chosenIdiom.composers) {
                $('#composer').append($('<option>', { 
                    value: composer,
                    text : composer
                }));
            }

            // save the index of the chosen idiom
            $("#idiomIndex").val(index);

            $("div#idioms").hide();
            $("div#idiomSetup").show();
        }
    };
}

/**
 * Saves the setup of the chosen idiom and sends a message to get the preview
 */
function saveIdiomSetup() {
    return function (event) {
        event.preventDefault();

        let currentState = vscode.getState();

        if(currentState !== undefined && currentState.idioms !== undefined){

            let idiomIndex = $("#idiomIndex").val();
            let idiom = currentState.idioms[idiomIndex];

            let outputSnippet = {
                name: $("#idiomName").val(),
                composer: $("#composer").val(),
                parameters: [],
                metavariables: [],
                removed: true,
                fragment: idiom.fragment,
                previewFragment: "",
                rootNodeType: idiom.rootNodeType
            };

            // save metavariable values
            var metavariableInputs = $(".metavariable");
            for (let metavariable of metavariableInputs) {

                // check if this metavariable name exists
                if (outputSnippet.parameters.indexOf($(metavariable).val()) !== -1) {
                    vscode.postMessage({
                        command: "showError",
                        parameters: {
                            error: "Metavariable names should be unique!"
                        }
                    });
                    return;
                }

                outputSnippet.parameters.push($(metavariable).val());
                outputSnippet.metavariables.push(metavariable.getAttribute('data-metavariable'));
            }

            let index = saveOutputSnippetToState(outputSnippet);
            
            vscode.postMessage({
                command: "getPreview",
                parameters: {
                    index: index,
                    fragment: outputSnippet.fragment,
                    parameters: outputSnippet.parameters,
                    metavariables: outputSnippet.metavariables
                }
            });
        
            // reset form and show idioms list again
            $("form#idiomSetupForm").trigger("reset");
            $("div#idiomMetavariables").empty();
            $("#composer").empty();

            $("div#idiomSetup").hide();
            $("div#idioms").show();
        }
    };
}

/**
 * Hides idiom setup form after cancelling and shows the list of idioms
 */
function closeIdiomSetup() {
    return function () {
        $("form#idiomSetupForm").trigger("reset");
        $("div#idiomMetavariables").empty();

        $("div#idiomSetup").hide();
        $("div#idioms").show();
    };
}

/**
 * Shows the preview of the output snippet
 * @param fragment 
 * @param index 
 */
function showOutputSnippetPreview(fragment, index) {
    let currentState = vscode.getState();

    if(currentState !== undefined && currentState.outputSnippets !== undefined){
        let snippet = currentState.outputSnippets[index];

        if (snippet !== undefined) {
            snippet.removed = false;
            snippet.previewFragment = fragment;
            showOutputSnippet(snippet, index);

            currentState.outputSnippets.splice(index, 1, snippet);

            vscode.setState(currentState);
        }
    } 
}

/**
 * Shows the output snippet in the preview
 * @param outputSnippets 
 */
function showOutputSnippet(outputSnippet, index) {
    let snippetHTML = 
    `<div class="output-snippet-div mb-3" id="output${index}">
        <div class="row">
            <div class="col-10">
                <h5>${outputSnippet.name}</h5>
                <p>${outputSnippet.composer}</p>
            </div>
            <div class="col-2">
                <button id="${index}" type="button" class="btn btn-default vscbtn-sm removeSnippet">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        </div>
        <div class="align-self-center fragment-snippet"><p>${outputSnippet.previewFragment}</p></div>
    </div>`;

    $("#output-snippets").append(snippetHTML);
}

/**
 * Saves idioms to the state
 * @param idioms 
 */
function saveIdiomsToState(idioms) {
    let currentState = vscode.getState();

    if(currentState === undefined){
        currentState = {};
    }
    currentState.idioms = idioms;

    vscode.setState(currentState);
}

/**
 * Saves the setup of the chosen idiom to the state
 * @param snippet 
 * @returns index of the output snippet
 */
function saveOutputSnippetToState(snippet) {
    let currentState = vscode.getState();

    if(currentState === undefined){
        currentState = {};
    }

    if(currentState.outputSnippets === undefined) {
        currentState.outputSnippets = [];
    }
    
    let length = currentState.outputSnippets.push(snippet);

    vscode.setState(currentState);

    return length-1;
}

/**
 * Removes the setup of the chosen idiom from the state and removes the preview from the output snippet list
 */
function removeOutputSnippetFromState() {
    return function () {
        let currentState = vscode.getState();

        if(currentState !== undefined && currentState.outputSnippets !== undefined){
            let index = $(this).attr('id');
            index.replace('output', '');

            let snippet = currentState.outputSnippets[index];

            if (snippet !== undefined){

                snippet.removed = true;

                vscode.setState(currentState);
                
                // remove output snippet from the preview
                $(`div.output-snippet-div#output${index}`).remove();
            }
        }
    };
}

function generate() {
    return function () {
        let currentState = vscode.getState();

        if(currentState !== undefined && currentState.outputSnippets !== undefined){
            vscode.postMessage({
                command: "generate",
                parameters: {
                    outputSnippets: currentState.outputSnippets
                }
            });
        }
        else {
            vscode.postMessage({
                command: "showError",
                parameters: {
                    error: "Output snippets list is empty!"
                }
            });
        }
    };
}

/**
 * Clears output snippets
 */
function clearOutputSnippets() {
    let currentState = vscode.getState();

    if(currentState !== undefined && currentState.outputSnippets !== undefined){
        currentState.outputSnippets.length = 0;
        vscode.setState(currentState);
    }    

    $("#output-snippets").empty();
       
    $("div#idiomSetup").hide();
    $("div#idioms").show();
}