import { WebviewTab } from "./WebviewTab";
import * as vscode from 'vscode';

export default class IdiomsWebviewTab extends WebviewTab {

    constructor(extensionPath: string, disposables: vscode.Disposable[]) {
        super();

        this._viewType = 'IdiomsTab';
        this._title = 'RoseLibML - Idioms';
        this._htmlPath = 'tab_idioms.html';
        this._jsPath = 'tab_idioms.js';
        this._extensionPath = extensionPath;
        this._disposables = disposables;
    }

    protected addMessageReceiver() {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'getIdioms':
							this.getIdioms(message.parameters);
							break;
						case 'getPreview':
							this.getPreview(message.parameters);
							break;
						case 'generate':
							this.generate(message.parameters);
							break;
						case 'showError':
							vscode.window.showErrorMessage(`Error: ${message.parameters.error}`);
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
	 * Executes the 'rose-lib-ml.getIdioms' command from the language server.
	 * If the command was executed successfully, a message with the results
	 * is sent to the webview in order to show the idioms.
	 * @param parameters 
	 */
    public getIdioms(
		parameters: {
			rootNodeType: string|null
		}
	) {
        vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({message: 'Loading idioms in progress', increment: 0});
	
			try {
				// execute get idioms command from the language server
				response = await vscode.commands.executeCommand('rose-lib-ml.getIdioms',
				{
					'RootNodeType': parameters.rootNodeType,
				});
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(`RoseLibML - ${response.message}`);
	
					// send a message to show idioms in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showIdioms', value: response.value});
					}
				}
			}
			catch (error) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
    }

	/**
	 * Executes the 'rose-lib-ml.getPreview' command from the language server.
	 * If the command was executed successfully, a message is sent to the 
	 * webview in order to show the preview of the output snippet.
	 * @param parameters 
	 */
	private getPreview(
		parameters: {
			fragment: string,
			metavariables: [],
			parameters: []
			index: number
		}
	) {
		let snippet = {
			fragment: parameters.fragment,
			methodParameters: Array()
		};

		// transform metavariables/parameters
		for(let i = 0; i < parameters.parameters.length; i++){
			snippet.methodParameters.push({
				parameter: parameters.parameters[i], 
				metavariable: parameters.metavariables[i]
			});
		}

		vscode.window.withProgress({
			location: vscode.ProgressLocation.Window,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;

			progress.report({message: 'Getting the output snippet preview', increment: 0});
		
			try {
				// execute getPreview command from the language server
				response = await vscode.commands.executeCommand(
					'rose-lib-ml.getPreview', 
					snippet
				);
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(`RoseLibML - ${response.message}`);

					// send a message to show output snippet in preview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showSnippetPreview', value: response.value, index: parameters.index});
					}
				}
			}
			catch (error) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
	}

	/**
	 * Executes the 'rose-lib-ml.generate' command from the language server.
	 * If the command was executed successfully, a message is sent to the 
	 * webview in order to clear the output snippets. 
	 * @param parameters 
	 */
    private generate(
		parameters: {
			outputSnippets: any[]
		}
	) {
		// prepare output snippets to send to language server
		let outputSnippets = this.prepareOutputSnippets(parameters.outputSnippets);

		if (outputSnippets.length === 0) {
			vscode.window.showErrorMessage('Output snippets list is empty!');
			return;
		}

		vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({message: 'Generating RoseLib methods in progress', increment: 0});
	
			try {
				// execute generate command from the language server
				response = await vscode.commands.executeCommand(
					'rose-lib-ml.generate', 
					outputSnippets
				);
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(`RoseLibML - ${response.message}`);

					// send a message to clear output snippets
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'clearOutputSnippets'});
					}
				}
			}
			catch (error) {
				progress.report({message: 'An error occured.', increment: 100 });
				vscode.window.setStatusBarMessage('RoseLibML - An error occured.');
				vscode.window.showErrorMessage('An error occurred. Please try again.');
			}
		});
	}

	/**
	 * Prepares output snippets to send them to the language server
	 * @param snippets 
	 * @returns outputSnippets
	 */
	private prepareOutputSnippets(snippets: any): any[] {
		let outputSnippets: any[] = [];

		// transform output snippets for language server
		for (let snippet of snippets) {
			if(snippet.removed) {
				continue;
			}

			let outputSnippet = {
				methodName: snippet.name,
				methodParameters: Array(),
				composer: snippet.composer,
				fragment: snippet.fragment,
				rootNodeType: snippet.rootNodeType
			};

			// transform metavariables/parameters
			for(let i = 0; i < snippet.parameters.length; i++){
				outputSnippet.methodParameters.push({
					parameter: snippet.parameters[i], 
					metavariable: snippet.metavariables[i]
				});
			}

			outputSnippets.push(outputSnippet);
		}

		return outputSnippets;
	}
}
