$(document).ready(function () {
    $("form#pCFGForm").submit(submitPCFGForm());

    const previousState = vscode.getState();

    if(previousState !== undefined) {
        showPCFGVisualization(previousState.probabilities);
    }
});

$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.command === 'showPCFG' && message.value !== null) {
        // save state - for panel restoring
        vscode.setState({probabilities: message.value});
        
        showPCFGVisualization(message.value);
    }
});

const vscode = acquireVsCodeApi();

function showPCFGVisualization(probabilities) {
    $("#probability-table > thead").empty();
    $("#probability-table > tbody").empty();

    $("#probability-table > thead").append("<tr><td>RULE</td><td>PROBABILITY</td></tr>");

    for (let key in probabilities) {
        let camelCaseKey = key.charAt(0).toUpperCase();
        let row = "<tr><td>" + camelCaseKey + key.slice(1) + "</td><td>" + probabilities[key] + "</td></tr>";
        $("#probability-table > tbody").append(row);
    }
}

function submitPCFGForm() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "runPCFG",
            parameters: {
                probabilityCoefficient: $("#probabilityCoefficient").val(),
                inputFolder: $("#inputFolder").val(),
                outputFile: $("#outputFile").val(),
            }
        });

        $("form#pCFGForm").trigger("reset");
    };
}
