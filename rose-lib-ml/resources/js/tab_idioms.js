$(document).ready(function () {
    $("button#generateButton").click(generate());

});

const vscode = acquireVsCodeApi();

function generate() {
    return function (event) {
        vscode.postMessage({
            command: "generate"
        });
    };
}