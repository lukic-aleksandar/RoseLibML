import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

export abstract class WebviewTab {
    
	protected _viewType!: string;
	protected _title!: string;
	protected _extensionPath!: string;
	protected _htmlPath!: string;
	protected _jsPath!: string;

	protected _disposables: vscode.Disposable[] = [];

	protected _panel: vscode.WebviewPanel | undefined;

	protected abstract addMessageReceiver(): void;
	
	/**
	 * Shows the existing Webview Panel or creates a new one if it doesn't exist yet
	 */
	public openTab() {
		if (this._panel) {
			this._panel.reveal(vscode.ViewColumn.Active);
		} else {
			this._panel = vscode.window.createWebviewPanel(this._viewType, this._title, vscode.ViewColumn.Active,
				{
					localResourceRoots: [
						vscode.Uri.file(path.join(this._extensionPath, 'resources', 'css')),
						vscode.Uri.file(path.join(this._extensionPath, 'resources', 'js'))
					],
					enableScripts: true
				});

			this.addMessageReceiver();

			this.setUpWebviewPanel();

			this._panel.onDidDispose(
				() => {
					this._panel = undefined;
				},
				null,
				this._disposables
			);
		}
	}

	/**
	 * Opens the dialog for choosing a folder or a file.
	 * Choosing a folder/file sends a message to the webview with the path 
	 * of the chosen folder/file and the form field which should be set.
	 * @param parameters 
	 */
	protected showOpenDialog(
		parameters: {
			selectFile: boolean, 
			field: string
		}
	) {
		const options: vscode.OpenDialogOptions = {
			canSelectMany: false,
			title: `Choose ${parameters.selectFile ? 'File' : 'Folder'}`,
			openLabel: 'Choose',
			canSelectFiles: parameters.selectFile,
			canSelectFolders: !parameters.selectFile
		};

		vscode.window.showOpenDialog(options).then(fileUri => {
			if (fileUri && fileUri[0] && this._panel !== undefined) {
				this._panel.webview.postMessage({command:'setPath', chosenFolder: fileUri[0].fsPath, inputField: parameters.field});
			}
		});
	}

	private setUpWebviewPanel() {
		if (this._panel !== undefined) {
			const htmlUri = vscode.Uri.file(path.join(this._extensionPath, 'resources', this._htmlPath));
			var html = fs.readFileSync(htmlUri.fsPath, 'utf8');
		
			const cssUri = vscode.Uri.file(path.join(this._extensionPath, 'resources/css', 'index.css'));
			const cssWebviewUri = this._panel.webview.asWebviewUri(cssUri);
		
			const jsUri = vscode.Uri.file(path.join(this._extensionPath, 'resources/js', this._jsPath));
			const jsWebviewUri = this._panel.webview.asWebviewUri(jsUri);
		
			this._panel.webview.html = html.replace('${styleUri}', cssWebviewUri.toString()).replace('${scriptUri}', jsWebviewUri.toString()).replace(/\$\{cspSource\}/g, this._panel.webview.cspSource);

			this._panel.iconPath = vscode.Uri.file(path.join(this._extensionPath,"resources/icons/rl_icon.svg"));
		}
	}
}
