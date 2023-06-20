# RoseLibML Visual Studio Code Extension

## Instructions to run locally

### Needed installations:

1. Install Node with NPM (if node and npm are not already installed)
2. Install tsc: `npm install -g typescript` (if typescript is not already installed)
3. Run NPM install inside the extension folder

### Running
1. Insert the path to the RoseLib solution in the [knowledge base](https://github.com/lukic-aleksandar/RoseLibML/blob/vsc-extension/RoseLibLS/knowledge_base.json)
2. Build the RoseLibLS solution. It will build the RoseLibML solution if needed. DO NOT RUN THE LS. The front-end runs the build using dotnet tool. The path to LS is provided in extension.ts, with serverPath variable. Make sure it does know where the build is. 
3. Open rose-lib-ml in Visual Studio Code and run with F5. This will compile and run the extension in a new Extension Development Host window.
4. Activate the extension by clicking on the new icon in the Activity Bar.