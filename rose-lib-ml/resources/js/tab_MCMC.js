$(document).ready(function () {
    $("form#MCMCForm").submit(submitMCMCForm());

    const previousState = vscode.getState();

    if(previousState !== undefined) {
        showMCMCVisualization(previousState.idiomsPerIteration);
    }
});


$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.command === 'showMCMC' && message.value !== null){   
        // save state - for panel restoring
        vscode.setState({idiomsPerIteration: message.value});
        
        showMCMCVisualization(message.value);
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
        
        for (let key in idiomsPerIteration) {
            let fragments = idiomsPerIteration[key];

            let iterationHTML = "<p class=\"mt-3 mb-3\"> ITERATION " + key + "</p>";

            $("#idioms-per-iteration").append(iterationHTML);

            for (var i = 0; i < fragments.length; i++) 
            { 
                let fragmentHTML = "<div class=\"fragment-snippet mt-3 mb-3\">" + fragments[i] + "</div>";
                $("#idioms-per-iteration").append(fragmentHTML);
            }
        }
}