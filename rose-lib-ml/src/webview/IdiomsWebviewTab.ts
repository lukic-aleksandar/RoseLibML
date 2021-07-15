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
						case 'chooseIdiom':
							this.chooseIdiom(message.parameters);
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

    public getIdioms(parameters: any) {
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
					'RootType': parameters.rootNodeType,
				});
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(response.message);
	
					// show idioms in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showIdioms', value: response.value});
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

	private async chooseIdiom(parameters: any) {
		// choose method name
		let methodName = await vscode.window.showInputBox(
			{
				title: "Insert method name",
				value: "ExampleMethod"
			}
		);

		if (methodName) {
			let metavariables = parameters.metavariables;

			let methodParameters = [];

			//choose method parameters
			for (let i = 0; i < metavariables.length; i++) {
				let mv = await vscode.window.showInputBox(
					{
						title: `Insert a name for the metavariable ${i+1}: ${metavariables[i]}`,
						value: `metavariable${i+1}`
					}
				);

				methodParameters.push(mv);
			}

			let confirmationTitle = `Generate method named ${methodName}`;
			if(metavariables.length !== 0){
				confirmationTitle += ` with parameters ${methodParameters.join(", ")}`;
			}
			confirmationTitle += `?`;

			// confirm and generate
			let result = await vscode.window.showQuickPick(
				['Yes', 'No'],
				{
					title: confirmationTitle,
				}
			);

			if(result === 'Yes'){
				this.generate({
					'ContextNodes': parameters.contextNodes,
					'RootCSType': parameters.rootCSType,
					'Fragment': parameters.fragment,
					'MethodName': methodName,
					'MethodParameters': methodParameters
				});
			}
		}
	}

    private generate(parameters: any) {
		vscode.window.withProgress({
			location: vscode.ProgressLocation.Notification,
			title: 'RoseLibML'
		}, async progress => {
	
			let response: any;
	
			progress.report({message: 'Generating RoseLib method in progress', increment: 0});
	
			try {
				// execute generate command from the language server
				response = await vscode.commands.executeCommand('rose-lib-ml.generate', parameters);
	
				progress.report({message: response.message, increment: 100 });
	
				if (response.error === true) {
					vscode.window.setStatusBarMessage('An error occured.');
					vscode.window.showErrorMessage(response.message);
				}
				else {
					vscode.window.setStatusBarMessage(response.message);

					// show output snippets in webview
					if (this._panel !== undefined){
						this._panel.webview.postMessage({command:'showOutputSnippets', value: response.value});
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