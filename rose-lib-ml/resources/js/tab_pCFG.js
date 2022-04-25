$(document).ready(function () {
    $("form#pCFGForm").submit(submitPCFGForm());

    // restoring the state of the panel (after changing tabs)
    const previousState = vscode.getState();

    if(previousState !== undefined && previousState.probabilities !== undefined) {
        showPCFGVisualization(previousState.probabilities);
    }

    $("button#chooseInputFolder").click(showOpenDialog('inputFolder', false));
    $("button#chooseOutputFolder").click(showOpenDialog('outputFolder', false));
});

$(window).on("message", function(e) {
    const message = e.originalEvent.data;

    if(message.value !== null) {
        switch(message.command){
            case 'showPCFG':
                // save state - for panel restoring
                vscode.setState({probabilities: message.value});
                
                showPCFGVisualization(message.value);
                break;
            case 'setPath':
                $(`input#${message.inputField}`).val(message.chosenFolder);
                break;
        }
        
    }
});

const vscode = acquireVsCodeApi();

function submitPCFGForm() {
    return function (event) {
        event.preventDefault();

        vscode.postMessage({
            command: "runPCFG",
            parameters: {
                probabilityCoefficient: $("#probabilityCoefficient").val(),
                inputFolder: $("#inputFolder").val(),
                outputFolder: $("#outputFolder").val(),
            }
        });

        $("form#pCFGForm").trigger("reset");
    };
}

function showPCFGVisualization(probabilities) {
    $("#probability-table > thead").empty();
    $("#probability-table > tbody").empty();

    $("#probability-table > thead").append("<tr><td>RULE</td><td>PROBABILITY</td></tr>");

    for (let rule in probabilities) {
        let firstChar = rule.charAt(0).toUpperCase();

        let row =  `<tr>
                        <td>${firstChar + rule.slice(1)}</td>
                        <td>${probabilities[rule]}</td>
                    </tr>`;
        
        $("#probability-table > tbody").append(row);
    }
}

function showOpenDialog(formField, file) {
    return function () {
        vscode.postMessage({
            command: "showOpenDialog",
            parameters: {
                field: formField,
                selectFile: file
            }
        });
    };
}
