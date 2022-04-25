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

    protected addMessageReceiver() {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'runMCMC':
							this.runMCMCPhase(message.parameters);
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
	 * Executes the 'rose-lib-ml.runMCMC' command from the language server.
	 * If the command was executed successfully, a message is shown to inform
	 * that the MCMC phase is in progress.
	 * @param parameters 
	 */
    private async runMCMCPhase(
		parameters: {
			inputFolder: string, 
			outputFolder: string, 
			pCFGFile: string, 
			iterations: number, 
			burnInIterations: number,
			initialCutProbability: number,
			alpha: number,
			threshold: number
		}
	) {
		let response: any;
	
		try {
			vscode.window.setStatusBarMessage('RoseLibML: MCMC phase in progress.');

			// execute MCMC command from the language server
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

			vscode.window.setStatusBarMessage('');
				
			if(response.error === true) {
				vscode.window.setStatusBarMessage('RoseLibML: An error occured during MCMC phase.', 10000);
				vscode.window.showErrorMessage(`RoseLibML: ${response.message}`);
			}
			else {
				vscode.window.setStatusBarMessage(`RoseLibML: ${response.message}`, 10000);
				vscode.window.showInformationMessage(`RoseLibML: ${response.message}`);
			}	
		}
		catch (error) {
			vscode.window.setStatusBarMessage('RoseLibML: An error occured during MCMC phase.', 10000);
			vscode.window.showErrorMessage('RoseLibML: An error occurred. Please try again.');
		}
	}

	/**
	 * Sends a message to show idioms per iteration in webview
	 * @param idiomsPerIteraton 
	 */
	public showIdiomsPerIteration(idiomsPerIteraton: any) {
		if (this._panel !== undefined){
			this._panel.webview.postMessage({command:'showMCMC', value: idiomsPerIteraton });
		}
	}
}
