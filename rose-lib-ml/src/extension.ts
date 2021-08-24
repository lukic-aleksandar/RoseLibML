import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';

import PCFGWebviewTab from './webview/PCFGWebviewTab';
import MCMCWebviewTab from './webview/MCMCWebviewTab';
import IdiomsWebviewTab from './webview/IdiomsWebviewTab';

var client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {

	let clientReady = false;

	let serverModule = context.asAbsolutePath(path.join('..', 'RoseLibLS', 'bin', 'Debug', 'net5.0', 'RoseLibLS.exe'));

	let serverOptions: ServerOptions = {
		run: { command: serverModule },
		debug: { command: serverModule }
	};

	let clientOptions: LanguageClientOptions = {
		documentSelector: ['*'],
		synchronize: {},
		progressOnInitialization: true,
		outputChannelName: 'RoseLibML'
	};

	// create and start the language client (language client launches the language server on start)
	client = new LanguageClient(
		'roseLibMLClient',
		'RoseLibML Client',
		serverOptions,
		clientOptions
	);

	client.onReady().then(() => {
		clientReady = true;
		vscode.window.setStatusBarMessage('RoseLibML - Language server initialized.');
	});

	let clientDisposable = client.start();

	vscode.window.setStatusBarMessage('RoseLibML - Language server initialization in progress.');

	let pCFGTab = new PCFGWebviewTab(context.extensionPath, context.subscriptions);
	let MCMCTab = new MCMCWebviewTab(context.extensionPath, context.subscriptions);
	let idiomsTab = new IdiomsWebviewTab(context.extensionPath, context.subscriptions);

	// open pCFG tab command
	let pCFGTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openPCFG', 
		() => {
			if(!clientReady){
				vscode.window.showWarningMessage("Language server initialization in progress. Please try again shortly.");
				return;
			}

			pCFGTab.openTab();
		}
	);

	// open MCMC tab command
	let MCMCTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openMCMC',
		() => {
			if(!clientReady){
				vscode.window.showWarningMessage("Language server initialization in progress. Please try again shortly.");
				return;
			}

			MCMCTab.openTab();
		}
	);

	// open idioms tab command
	let idiomsTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openIdioms', 
		() => {
			if(!clientReady){
				vscode.window.showWarningMessage("Language server initialization in progress. Please try again shortly.");
				return;
			}
			
			idiomsTab.openTab();
		}
	);

	context.subscriptions.push(clientDisposable, pCFGTabCommand, MCMCTabCommand, idiomsTabCommand);
}

export function deactivate() {
	if (!client) {
		return undefined;
	}

	return client.stop();
}
