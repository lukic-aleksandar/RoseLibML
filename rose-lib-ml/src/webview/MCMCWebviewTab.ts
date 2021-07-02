import { WebviewTab } from "./WebviewTab";
import * as vscode from 'vscode';

export default class MCMCWebviewTab extends WebviewTab {

    constructor(extensionPath: string, disposables: vscode.Disposable[]) {
        super();

        this._viewType = 'MCMCTab';
        this._title = 'RoseLibML - MCMC';
        this._htmlPath = 'tab_MCMC.html';
		this._jsPath = 'tab_MCMC.js';
        this._extensionPath = extensionPath;
        this._disposables = disposables;
    }

    protected addMessageReceiver(): void {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'runMCMC':
							this.runMCMCPhase(message.parameters);
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

    private runMCMCPhase(parameters: any){
		vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({ message: 'MCMC phase in progress', increment: 0 });
	
			try {
				// execute MCMC command from language server
				response = await vscode.commands.executeCommand('rose-lib-ml.runMCMC',
					{
						'InputFolder': parameters.inputFolder,
						'PCFGFile': parameters.pCFGFile,
						'Iterations': parameters.iterations,
						'BurnInIterations': parameters.burnInIterations,
						'InitialCutProbability': parameters.initialCutProbability,
						'Alpha': parameters.alpha,
						'Threshold': parameters.threshold,
						'OutputFolder': parameters.outputFolder,
					}
				);
	
				progress.report({message: response.message, increment: 100 });
				
				if(response.error === true) {
					vscode.window.setStatusBarMessage('An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(response.message);

					// show idioms per iteration in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showMCMC', value: response.value });
					}

				}
	
			}
			catch (error) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
	}
}