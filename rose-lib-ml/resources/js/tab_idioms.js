$(document).ready(function () {
    $("form#idiomsForm").submit(filterIdiomsByRootType());
    
    $("div#idioms-list").on("click", "button.chooseIdiom", chooseIdiom());

    // restoring the state of the panel (after changing tabs)
    const previousState = vscode.getState();

    if(previousState !== undefined) {
        showIdioms(previousState.idioms);
    }
});

$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.value !== null){
        switch(message.command){
            case 'showIdioms':
                 // save state - for panel restoring
                vscode.setState({idioms: message.value});
                
                showIdioms(message.value);
                break;
            case 'showOutputSnippets':
                showOutputSnippets(message.value);
                break;
        }
    }
});

const vscode = acquireVsCodeApi();

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

function chooseIdiom() {
    return function (event) {
        const previousState = vscode.getState();

        if(previousState !== undefined) {
            let chosenIdiom = previousState.idioms[$(this).attr('id')];

            vscode.postMessage({
                command: "chooseIdiom",
                parameters: chosenIdiom
            });
        }
    };
}

function showIdioms(idioms) {
    $("#idioms-list").empty();
        
    for (let [i, idiom] of idioms.entries()) {
        let idiomHTML = "<div class=\"row mb-2\"><div class=\"col-10 align-self-center fragment-snippet mt-3 mb-3\"><p>" + idiom.fragment + "</p></div>";
        idiomHTML += "<div class=\"col-2 align-self-center\"><button id=\"" + i + "\" type=\"button\" class=\"btn btn-default vscbtn-sm chooseIdiom\"><i class=\"fas fa-play fa-lg\"></i></button></div></div>";

        $("#idioms-list").append(idiomHTML);
    } 
}

function showOutputSnippets(outputSnippets) {
    $("#output-snippets").empty();

    for(let outputSnippet of outputSnippets) {
        let snippetHTML = "<div class=\"align-self-center mb-3 fragment-snippet\"><p>" + outputSnippet + "</p></div>";

        $("#output-snippets").append(snippetHTML);
    }
}
