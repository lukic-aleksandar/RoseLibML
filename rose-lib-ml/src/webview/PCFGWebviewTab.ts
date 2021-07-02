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

    protected addMessageReceiver(): void {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'runPCFG':
							this.runPCFGPhase(message.parameters);
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

    private runPCFGPhase(parameters: any) : any {
		vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({ message: 'pCFG phase in progress', increment: 0 });
	
			try {
				// execute pCFG command from language server
				response = await vscode.commands.executeCommand('rose-lib-ml.runPCFG',
					{
						'ProbabilityCoefficient': parameters.probabilityCoefficient,
						'InputFolder': parameters.inputFolder, 
						'OutputFile': parameters.outputFile
					}
				);
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(response.message);
	
					// show pCFG visualization in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showPCFG', value: response.value});
					}
				}
				
			}
			catch (_) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
	}
}