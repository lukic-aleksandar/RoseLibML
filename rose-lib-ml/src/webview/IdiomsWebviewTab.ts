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
							vscode.window.showErrorMessage(`RoseLibML: ${message.parameters.error}`);
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
    public async getIdioms(
		parameters: {
			rootNodeType: string|null
		}
	) {	
		let response: any;
	
		try {
			vscode.window.setStatusBarMessage('RoseLibML: Getting idioms in progress.', 10000);

			// execute get idioms command from the language server
			response = await vscode.commands.executeCommand('rose-lib-ml.getIdioms',
				{
					'RootNodeType': parameters.rootNodeType,
				}
			);

			vscode.window.setStatusBarMessage('');
	
			if (response.error === true) {
				vscode.window.setStatusBarMessage('RoseLibML: An error occured while getting idioms.', 10000);
				vscode.window.showErrorMessage(`RoseLibML: ${response.message}`);
			}
			else {
				vscode.window.setStatusBarMessage(`RoseLibML: ${response.message}`, 10000);

				// send a message to show idioms in webview
				if (this._panel !== undefined){
					this._panel.webview.postMessage({command:'showIdioms', value: response.value});
				}
			}
		}
		catch (error) {
			vscode.window.setStatusBarMessage('RoseLibML: An error occured while getting idioms.', 10000);
			vscode.window.showErrorMessage('RoseLibML: An error occurred. Please try again.');
		}
    }

	/**
	 * Executes the 'rose-lib-ml.getPreview' command from the language server.
	 * If the command was executed successfully, a message is sent to the 
	 * webview in order to show the preview of the output snippet.
	 * @param parameters 
	 */
	private async getPreview(
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
	
		let response: any;
	
		try {
			vscode.window.setStatusBarMessage('RoseLibML: Getting preview in progress.');

			// execute getPreview command from the language server
			response = await vscode.commands.executeCommand(
				'rose-lib-ml.getPreview', 
				snippet
			);

			vscode.window.setStatusBarMessage('');
	
			if (response.error === true) {
				vscode.window.setStatusBarMessage('RoseLibML: An error occured while getting the preview.', 10000);
				vscode.window.showErrorMessage(`RoseLibML: ${response.message}`);
			}
			else {
				vscode.window.setStatusBarMessage(`RoseLibML: ${response.message}`, 10000);

				// send a message to show output snippet in preview
				if (this._panel !== undefined){
					this._panel.webview.postMessage({command:'showSnippetPreview', value: response.value, index: parameters.index});
				}
			}
		}
		catch (error) {
			vscode.window.setStatusBarMessage('RoseLibML: An error occured while getting the preview.', 10000);
			vscode.window.showErrorMessage('RoseLibML: An error occurred. Please try again.');
		}
	}

	/**
	 * Executes the 'rose-lib-ml.generate' command from the language server.
	 * If the command was executed successfully, a message is sent to the 
	 * webview in order to clear the output snippets. 
	 * @param parameters 
	 */
    private async generate(
		parameters: {
			outputSnippets: any[]
		}
	) {
		// prepare output snippets to send to language server
		let outputSnippets = this.prepareOutputSnippets(parameters.outputSnippets);

		if (outputSnippets.length === 0) {
			vscode.window.showErrorMessage('RoseLibML: Output snippets list is empty!');
			return;
		}
	
		let response: any;
	
		try {
			vscode.window.setStatusBarMessage('RoseLibML: Generating methods is in progress.');

			// execute generate command from the language server
			response = await vscode.commands.executeCommand(
				'rose-lib-ml.generate', 
				outputSnippets
			);

			vscode.window.setStatusBarMessage('');
	
			if (response.error === true) {
				vscode.window.setStatusBarMessage('RoseLibML: An error occured while generating.', 10000);
				vscode.window.showErrorMessage(`RoseLibML: ${response.message}`);
			}
			else {
				vscode.window.setStatusBarMessage(`RoseLibML: ${response.message}`, 10000);
				vscode.window.showInformationMessage(`RoseLibML: ${response.message}`);

				// send a message to clear output snippets
				if (this._panel !== undefined){
					this._panel.webview.postMessage({command:'clearOutputSnippets'});
				}
			}
		}
		catch (error) {
			vscode.window.setStatusBarMessage('RoseLibML - An error occured while generating.', 10000);
			vscode.window.showErrorMessage('RoseLibML: An error occurred. Please try again.');
		}
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
