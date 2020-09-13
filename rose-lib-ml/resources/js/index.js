$(document).ready(function () {
    $("form#pCFGForm").submit(submitPCFGForm());
    $("form#MCMCForm").submit(submitMCMCForm());
    $("button#generateButton").click(generate());
});

const vscode = acquireVsCodeApi();

function submitPCFGForm() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "submit",
            parameters: {
                probabilityCoefficient: $("#probabilityCoefficient").val(),
                inputFolder: $("#inputFolder").val(),
                outputFile: $("#outputFile").val(),
            },
        });

        $("form#pCFGForm").trigger("reset");
    };
}

function submitMCMCForm() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "submit",
            parameters: {
                inputFolder: $("#inputFolder").val(),
                pCFGFile: $("#pCFGFile").val(),
                iterations: $("#iterations").val(),
                burnInIterations: $("#burnInIterations").val(),
                initialCutProbability: $("#initialCutProbability").val(),
                alpha: $("#alpha").val(),
                outputFolder: $("#outputFolder").val()
            },
        });

        $("form#MCMCForm").trigger("reset");
    };
}

function generate() {
    return function (event) {
        vscode.postMessage({
            command: "generate"
        });
    };
}