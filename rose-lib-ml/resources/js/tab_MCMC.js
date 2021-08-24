$(document).ready(function () {
    $("form#MCMCForm").submit(submitMCMCForm());

    $("button#chooseInputFolder").click(showOpenDialog('inputFolder', false));
    $("button#chooseOutputFolder").click(showOpenDialog('outputFolder', false));
    $("button#choosePCFGFile").click(showOpenDialog('pCFGFile', true));

    // restoring the state of the panel (after changing tabs)
    const previousState = vscode.getState();

    if(previousState !== undefined && previousState.idiomsPerIteration !== undefined) {
        showMCMCVisualization(previousState.idiomsPerIteration);
    }
});

$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.value !== null) {
        switch(message.command){
            case 'showMCMC':
                // save state - for panel restoring
                vscode.setState({idiomsPerIteration: message.value});
                
                showMCMCVisualization(message.value);
                break;
            case 'setPath':
                $(`input#${message.inputField}`).val(message.chosenFolder);
                break;
        }
    }
});

const vscode = acquireVsCodeApi();

function submitMCMCForm() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "runMCMC",
            parameters: {
                inputFolder: $("#inputFolder").val(),
                pCFGFile: $("#pCFGFile").val(),
                iterations: $("#iterations").val(),
                burnInIterations: $("#burnInIterations").val(),
                initialCutProbability: $("#initialCutProbability").val(),
                alpha: $("#alpha").val(),
                threshold: $("#threshold").val(),
                outputFolder: $("#outputFolder").val()
            }
        });

        $("form#MCMCForm").trigger("reset");
    };
}

function showMCMCVisualization(idiomsPerIteration) {
    $("#idioms-per-iteration").empty();
        
    for (let iteration in idiomsPerIteration) {
        let idioms = idiomsPerIteration[iteration];

        let iterationHTML = `<p class="mt-3 mb-3"> ITERATION ${iteration}</p>`;
        $("#idioms-per-iteration").append(iterationHTML);

        for (var i = 0; i < idioms.length; i++) 
        { 
            let idiomHTML = `<div class="fragment-snippet mt-3 mb-3"><p>${idioms[i]}</p></div>`;
            $("#idioms-per-iteration").append(idiomHTML);
        }
    }
}

function showOpenDialog(formField, files) {
    return function () {
        vscode.postMessage({
            command: "showOpenDialog",
            parameters: {
                field: formField,
                selectFile: files
            }
        });
    };
}
