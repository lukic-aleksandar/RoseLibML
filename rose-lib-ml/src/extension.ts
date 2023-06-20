import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions, WorkDoneProgress } from 'vscode-languageclient';

import PCFGWebviewTab from './webview/PCFGWebviewTab';
import MCMCWebviewTab from './webview/MCMCWebviewTab';
import IdiomsWebviewTab from './webview/IdiomsWebviewTab';

var client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {

	let pCFGTab = new PCFGWebviewTab(context.extensionPath, context.subscriptions);
	let MCMCTab = new MCMCWebviewTab(context.extensionPath, context.subscriptions);
	let idiomsTab = new IdiomsWebviewTab(context.extensionPath, context.subscriptions);

	let clientReady = false;

	let serverCommand = "dotnet";
	let serverPath = context.asAbsolutePath(path.join('..', 'RoseLibLS', 'bin', 'Debug', 'net6.0', 'RoseLibLS.dll'));

	let serverOptions: ServerOptions = {
        run: { command: serverCommand, args: [serverPath] },
        debug: { command: serverCommand, args: [serverPath] }
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
		vscode.window.setStatusBarMessage('');
		vscode.window.setStatusBarMessage('RoseLibML: Language server initialized.', 10000);

		client.onNotification("window/showMessage", (params) => {
			if(params.type === 'showMCMC'){
				MCMCTab.showIdiomsPerIteration(params.value);
				vscode.window.showInformationMessage(`RoseLibML: ${params.message}`);
			}
		});

		client.onRequest("window/workDoneProgress/create", (params) => {
			registerProgressHandler(client, params.token);
		});
	});

	let clientDisposable = client.start();

	vscode.window.setStatusBarMessage('RoseLibML: Language server initialization in progress.');

	// open pCFG tab command
	let pCFGTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openPCFG', 
		() => {
			if(!clientReady){
				vscode.window.showWarningMessage('RoseLibML: Language server initialization in progress. Please try again shortly.');
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
				vscode.window.showWarningMessage('RoseLibML: Language server initialization in progress. Please try again shortly.');
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
				vscode.window.showWarningMessage('RoseLibML: Language server initialization in progress. Please try again shortly.');
				return;
			}
			
			idiomsTab.openTab();
		}
	);

	context.subscriptions.push(clientDisposable, pCFGTabCommand, MCMCTabCommand, idiomsTabCommand);
}

export function registerProgressHandler(client: LanguageClient, token: string) {
	client.onProgress(WorkDoneProgress.type, token, value => {
		switch (value.kind) {
		  case 'begin':
			vscode.window.setStatusBarMessage(`RoseLibML: ${value.message}`);
			break;
		  case 'report':
			vscode.window.setStatusBarMessage(`RoseLibML: ${value.message}`);
		  case 'end':
			if(value.message !== undefined) {
				vscode.window.setStatusBarMessage(`RoseLibML: ${value.message}`);
			}
		}
	});
}

export function deactivate() {
	if (!client) {
		return undefined;
	}

	return client.stop();
}