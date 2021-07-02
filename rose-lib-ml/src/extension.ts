import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';
import PCFGWebviewTab from './webview/PCFGWebviewTab';
import MCMCWebviewTab from './webview/MCMCWebviewTab';
import IdiomsWebviewTab from './webview/IdiomsWebviewTab';

var client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {

	let serverModule = context.asAbsolutePath(path.join('..', 'RoseLibML', 'bin', 'Debug', 'RoseLibML.exe'));

	let serverOptions: ServerOptions = {
		run: { command: serverModule },
		debug: { command: serverModule }
	};

	let clientOptions: LanguageClientOptions = {
		documentSelector: ['*'],
		synchronize: {},
		outputChannelName: 'RoseLibML'
	};

	// create and start the language client
	client = new LanguageClient(
		'roseLibMLClient',
		'RoseLibML Client',
		serverOptions,
		clientOptions
	);

	let clientDisposable = client.start();

	let pCFGTab = new PCFGWebviewTab(context.extensionPath, context.subscriptions);
	let MCMCTab = new MCMCWebviewTab(context.extensionPath, context.subscriptions);
	let idiomsTab = new IdiomsWebviewTab(context.extensionPath, context.subscriptions);

	// open pCFG tab command
	let pCFGTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openPCFG', 
		() => {
			pCFGTab.openTab();
		}
	);

	// open MCMC tab command
	let MCMCTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openMCMC',
		() => {
			MCMCTab.openTab();
		}
	);

	// open idioms tab command
	let idiomsTabCommand = vscode.commands.registerCommand(
		'rose-lib-ml.openIdioms', 
		() => {
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
