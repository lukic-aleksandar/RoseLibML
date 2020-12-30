import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';

const tabs = {
	'pCFG': 'tab_PCFG.html',
	'MCMC': 'tab_MCMC.html',
	'Idioms': 'tab_idioms.html'
};

let client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {

	let pCFGPanel: vscode.WebviewPanel | undefined = undefined;
	let MCMCPanel: vscode.WebviewPanel | undefined = undefined;
	let idiomsPanel: vscode.WebviewPanel | undefined = undefined;

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

	// open pCFG tab command
	let pCFGTabCommand = vscode.commands.registerCommand('rose-lib-ml.openPCFG', () => {
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
					if (message.command === 'submit') {
						runPCFG(message.parameters);
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
	});

	// open MCMC tab command
	let MCMCTabCommand = vscode.commands.registerCommand('rose-lib-ml.openMCMC', () => {
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
				async message => {
					if (message.command === 'submit') {
						runMCMC(message.parameters);
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
	});

	// open idioms tab command
	let idiomsTabCommand = vscode.commands.registerCommand('rose-lib-ml.openIdioms', () => {
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
					if (message.command === 'generate') {
						// vscode.commands.executeCommand('rose-lib-ml.generate');
						vscode.window.showInformationMessage('Not implemented yet!');
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
	});

	context.subscriptions.push(clientDisposable, pCFGTabCommand, MCMCTabCommand, idiomsTabCommand);
}

function runPCFG(parameters: any) {
	vscode.window.withProgress({
		location: vscode.ProgressLocation.Window,
		title: 'RoseLibML'
	}, async progress => {

		let response: any;

		progress.report({ message: 'calculating probabilities', increment: 0 });

		try {
			response = await vscode.commands.executeCommand('rose-lib-ml.runPCFG',
				{
					'ProbabilityCoefficient': parameters.probabilityCoefficient,
					'InputFolder': parameters.inputFolder,
					'OutputFile': parameters.outputFile
				}
			);

			progress.report({ increment: 100 });

			if(response.value === true) {
				vscode.window.showInformationMessage(response.message);
			}
			else {
				vscode.window.showErrorMessage(response.message);
			}
			
		}
		catch (_) {
			progress.report({ increment: 100 });
			vscode.window.showErrorMessage('An error occurred. Please try again.');
		}
	});
}

function runMCMC(parameters: any) {
	vscode.window.withProgress({
		location: vscode.ProgressLocation.Window,
		title: 'RoseLibML'
	}, async progress => {

		let response: any;

		progress.report({ message: 'running MCMC', increment: 0 });

		try {
			response = await vscode.commands.executeCommand('rose-lib-ml.runMCMC',
				{
					'InputFolder': parameters.inputFolder,
					'PCFGFile': parameters.pCFGFile,
					'Iterations': parameters.iterations,
					'BurnInIterations': parameters.burnInIterations,
					'InitialCutProbability': parameters.initialCutProbability,
					'Alpha': parameters.alpha,
					'OutputFolder': parameters.outputFolder,
				}
			);

			progress.report({ increment: 100 });
			
			if(response.value === true) {
				vscode.window.showInformationMessage(response.message);
			}
			else {
				vscode.window.showErrorMessage(response.message);
			}

		}
		catch (_) {
			progress.report({ increment: 100 });
			vscode.window.showErrorMessage('An error occurred. Please try again.');
		}
	});
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

export function deactivate() {
	if (!client) {
		return undefined;
	}

	return client.stop();
}
