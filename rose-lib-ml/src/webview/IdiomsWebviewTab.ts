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

    protected addMessageReceiver(): void {
        if (this._panel !== undefined) {
            this._panel.webview.onDidReceiveMessage(
				message => {
					switch(message.command) {
						case 'generate':
							this.runIdioms(message.parameters);
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

    private runIdioms(parameters: any) {
		vscode.window.showInformationMessage('Not implemented yet!');
	}
}