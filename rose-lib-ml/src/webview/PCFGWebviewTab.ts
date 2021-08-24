import { WebviewTab } from "./WebviewTab";
import * as vscode from 'vscode';

export default class PCFGWebviewTab extends WebviewTab {

    constructor(extensionPath: string, disposables: vscode.Disposable[]) {
        super();

        this._viewType = 'pCFGTab';
        this._title = 'RoseLibML - pCFG';
        this._htmlPath = 'tab_pCFG.html';
		this._jsPath = 'tab_pCFG.js';
        this._extensionPath = extensionPath;
        this._disposables = disposables;
    }

    protected addMessageReceiver() {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'runPCFG':
							this.runPCFGPhase(message.parameters);
							break;
						case 'showOpenDialog':
							this.showOpenDialog(message.parameters);
							break;
						default:
							break;
					}
				},
				undefined,
				this._disposables
			);
        }
    }

	/**
	 * Executes the 'rose-lib-ml.runPCFG' command from the language server.
	 * If the command was executed successfully, a message with the results
	 * is sent to the webview in order to show the visualization of pCFG.
	 * @param parameters 
	 */
    private runPCFGPhase(
		parameters: {
			probabilityCoefficient: number, 
			inputFolder: string, 
			outputFolder: string
		}
	) {
		vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({ message: 'pCFG phase in progress', increment: 0 });
	
			try {
				// execute pCFG command from the language server
				response = await vscode.commands.executeCommand('rose-lib-ml.runPCFG',
					{
						'ProbabilityCoefficient': parameters.probabilityCoefficient,
						'InputFolder': parameters.inputFolder, 
						'OutputFolder': parameters.outputFolder
					}
				);
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(`RoseLibML - ${response.message}`);
	
					// send a message to show pCFG visualization in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showPCFG', value: response.value});
					}
				}			
			}
			catch (_) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
	}
}
