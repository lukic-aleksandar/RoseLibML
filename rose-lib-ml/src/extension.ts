import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

const tabs = {
	'pCFG': 'tab_PCFG.html',
	'MCMC': 'tab_MCMC.html',
	'Idioms': 'tab_idioms.html'
};

export function activate(context: vscode.ExtensionContext) {

	let pCFGPanel: vscode.WebviewPanel | undefined = undefined;
	let MCMCPanel: vscode.WebviewPanel | undefined = undefined;
	let idiomsPanel: vscode.WebviewPanel | undefined = undefined;

	context.subscriptions.push(
		// open pCFG tab 
		vscode.commands.registerCommand('rose-lib-ml.openPCFG', () => {
			if (pCFGPanel) {
				pCFGPanel.reveal(vscode.ViewColumn.Active);
			} else {
				pCFGPanel = vscode.window.createWebviewPanel('pCFGTab', 'RoseLibML - pCFG', vscode.ViewColumn.Active,
					{
						localResourceRoots: [
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'css')),
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'js'))
						],
						enableScripts: true
					});

				setUpWebviewPanel(context.extensionPath, pCFGPanel, 'pCFG');

				pCFGPanel.webview.onDidReceiveMessage(
					message => {
						switch (message.command) {
							case 'submit':
								// vscode.commands.executeCommand('rose-lib-ml.runPCFG');
								vscode.window.showInformationMessage('Run pCFG!');
								return;
						}
					},
					undefined,
					context.subscriptions
				);

				pCFGPanel.onDidDispose(
					() => {
						pCFGPanel = undefined;
					},
					null,
					context.subscriptions
				);
			}
		}),
		vscode.commands.registerCommand('rose-lib-ml.openMCMC', () => {
			// open MCMC tab
			if (MCMCPanel) {
				MCMCPanel.reveal(vscode.ViewColumn.Active);
			} else {
				MCMCPanel = vscode.window.createWebviewPanel('MCMCTab', 'RoseLibML - MCMC', vscode.ViewColumn.Active,
					{
						localResourceRoots: [
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'css')),
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'js'))
						],
						enableScripts: true
					});

				setUpWebviewPanel(context.extensionPath, MCMCPanel, 'MCMC');

				MCMCPanel.webview.onDidReceiveMessage(
					message => {
						switch (message.command) {
							case 'submit':
								// vscode.commands.executeCommand('rose-lib-ml.runMCMC');
								vscode.window.showInformationMessage('Run MCMC!');
								return;
						}
					},
					undefined,
					context.subscriptions
				);

				MCMCPanel.onDidDispose(
					() => {
						MCMCPanel = undefined;
					},
					null,
					context.subscriptions
				);
			}
		}),
		vscode.commands.registerCommand('rose-lib-ml.openIdioms', () => {
			// open idioms tab
			if (idiomsPanel) {
				idiomsPanel.reveal(vscode.ViewColumn.Active);
			} else {
				idiomsPanel = vscode.window.createWebviewPanel('IdiomsTab', 'RoseLibML - Idioms', vscode.ViewColumn.Active,
					{
						localResourceRoots: [
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'css')),
							vscode.Uri.file(path.join(context.extensionPath, 'resources', 'js'))
						],
						enableScripts: true
					});

				setUpWebviewPanel(context.extensionPath, idiomsPanel, 'Idioms');

				idiomsPanel.webview.onDidReceiveMessage(
					message => {
						switch (message.command) {
							case 'generate':
								// vscode.commands.executeCommand('rose-lib-ml.generate');
								vscode.window.showInformationMessage('Generate!');
								return;
						}
					},
					undefined,
					context.subscriptions
				);

				idiomsPanel.onDidDispose(
					() => {
						idiomsPanel = undefined;
					},
					null,
					context.subscriptions
				);
			}
		})
	);
}

function setUpWebviewPanel(extensionPath: string, panel: vscode.WebviewPanel, tab: keyof typeof tabs) {
	const htmlUri = vscode.Uri.file(path.join(extensionPath, 'resources', tabs[tab]));
	var html = fs.readFileSync(htmlUri.fsPath, 'utf8');

	const cssUri = vscode.Uri.file(path.join(extensionPath, 'resources/css', 'index.css'));
	const cssWebviewUri = panel.webview.asWebviewUri(cssUri);

	const jsUri = vscode.Uri.file(path.join(extensionPath, 'resources/js', 'index.js'));
	const jsWebviewUri = panel.webview.asWebviewUri(jsUri);

	panel.webview.html = html.replace('${styleUri}', cssWebviewUri.toString()).replace('${scriptUri}', jsWebviewUri.toString()).replace(/\$\{cspSource\}/g, panel.webview.cspSource);
}

export function deactivate() { }