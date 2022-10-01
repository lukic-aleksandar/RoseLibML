const vscode = acquireVsCodeApi();

let UIStates = {
    INITIAL: 0,
    CALCULATING: 1,
    SHOWING: 2,
}

function createEmptyRuleFrames(){
    return {
        frameNumber: 0,
        items: [],
        totalRules: 0,
        totalRuleFrames: 0
    }
}

//#region Page setup

$(document).ready(function () {
    setEventHandlers();
    restoreState();
});

function setEventHandlers() {
    $("button#chooseInputFolder").click(showOpenDialog('inputFolder', false));
    $("button#chooseOutputFolder").click(showOpenDialog('outputFolder', false));
    $("form#pCFGForm").submit(submitPCFGForm());
    $("form#searchRulesForm").submit(submitSearchRulesForm());
    $("button#loadMoreRules").click(loadMorePCFGRules());
}

//#endregion

//#region Window state handling
// Ako je počela kalkulacija, nemam info o tome. Ako plugin u tom trenutku ode u background, nema ti spasa :D Malo štucanje app... Popraviti
function restoreState() {
    const previousState = vscode.getState();

    if (!previousState) {
        applyUIState(UIStates.INITIAL);
        return;
    }

    let parameters = previousState.parameters;
    let ruleFrames = previousState.ruleFrames;

    if (parameters) {
        showPCFGParameters(parameters);
    }

    if (ruleFrames) {
        showPCFGVisualization(ruleFrames);
        applyUIState(UIStates.SHOWING);
    }
}

function applyUIState(UIState) {
    switch (UIState) {
        case UIStates.INITIAL:
            $("#submitText").show();
            $("#submitSpinner").hide();
            $("#pCFGVisualization").hide();
            break;
        case UIStates.CALCULATING:
            $("#submitText").hide();
            $("#submitSpinner").show();
            $("#pCFGVisualization").hide();
            break;
        case UIStates.SHOWING:
            $("#submitText").show();
            $("#submitSpinner").hide();
            $("#pCFGVisualization").show();
            break;
        default:
            throw new Error("Undefined UI State");
    }
}

//#endregion

//#region UI handling

function showPCFGParameters(parameters) {
    $("#probabilityCoefficient").val(parameters.probabilityCoefficient);
    $("#inputFolder").val(parameters.inputFolder);
    $("#outputFolder").val(parameters.outputFolder);
}

function showPCFGVisualization(ruleFrames) {
    $("#probability-table > thead").empty();
    $("#probability-table > tbody").empty();

    $("#probability-table > thead").append("<tr><td>RULE</td><td>PROBABILITY</td></tr>");

    for (let item of ruleFrames.items) {

        let row = `<tr>
                        <td>${item.rule}</td>
                        <td>${item.probability}</td>
                    </tr>`;

        $("#probability-table > tbody").append(row);
    }

    $("#loadMoreText").text(`Showing ${ruleFrames.items.length} out of ${ruleFrames.totalRules}`);
}

function removePCFGVisualisation() {
    $("#probability-table > thead").empty();
    $("#probability-table > tbody").empty();

    $("#probability-table > thead").append("<tr><td>RULE</td><td>PROBABILITY</td></tr>");
    $("#loadMoreText").text(``);
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

//#endregion

//#region Event handlers

function submitPCFGForm() {
    return function (event) {
        event.preventDefault();

        let setParameters = {
            probabilityCoefficient: $("#probabilityCoefficient").val(),
            inputFolder: $("#inputFolder").val(),
            outputFolder: $("#outputFolder").val(),
        }

        let newState = {};
        let previousState = vscode.getState();
        Object.assign(newState, previousState);
        newState["parameters"] = setParameters;
        vscode.setState(newState);

        vscode.postMessage({
            command: "runPCFGCalculation",
            parameters: setParameters
        });

        $("#submitText").hide();
        $("#submitSpinner").show();
    };
}

function submitSearchRulesForm() {
    return function (event) {
        event.preventDefault();

        let ruleStartsWith = $("#ruleStartsWith").val();

        let newState = {};
        let previousState = vscode.getState();
        Object.assign(newState, previousState);
        newState["ruleFrames"] = createEmptyRuleFrames();

        vscode.setState(newState);

        removePCFGVisualisation();

        vscode.postMessage({
            command: "loadPCFGRules",
            parameters: {neededFrame: 0, ruleStartsWith: ruleStartsWith}
        });
    };
}

function loadMorePCFGRules() {
    return function (event) {
        event.preventDefault();

        let ruleStartsWith = $("#ruleStartsWith").val();

        let state = vscode.getState();
        let framesLoadedSoFar = state["ruleFrames"].frameNumber;

        vscode.postMessage({
            command: "loadPCFGRules",
            parameters: {neededFrame: framesLoadedSoFar + 1, ruleStartsWith: ruleStartsWith}
        });
    }
}

$(window).on("message", function (e) {
    const message = e.originalEvent.data;

    if (message.value !== null) {
        switch (message.command) {
            case 'showPCFGRules':
                {
                    let newState = {};
                    let previousState = vscode.getState();
                    Object.assign(newState, previousState);

                    let ruleFrame = message.value;
                    if(ruleFrame.frameNumber == 0){
                        newState.ruleFrames = ruleFrame;
                    }
                    else{
                        if(previousState.ruleFrames){
                            newState.ruleFrames.items = ruleFrame.items.concat(newState.ruleFrames.items);
                            newState.ruleFrames.totalRules = ruleFrame.totalRules;
                            newState.ruleFrames.totalRuleFrames = ruleFrame.totalRuleFrames;
                            newState.ruleFrames.frameNumber = ruleFrame.frameNumber;
                        }
                        else{
                            throw new Error("Frames do not exist, but they should...");
                        }
                    }

                    vscode.setState(newState);

                    applyUIState(UIStates.SHOWING);
                    showPCFGVisualization(newState.ruleFrames);
                }

                break;

            case 'setPath':
                $(`input#${message.inputField}`).val(message.chosenFolder);
                break;
        }

    }
});

//#endregion