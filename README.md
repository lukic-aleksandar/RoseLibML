# RoseLibML

## The project

This project aims to enable inference of code idioms from an unlabeled data set.
The architecture of the solution is modular. The inference core can be extended to suport an arbitrary language.
Currently, the solution supports inference from files written in C# language, using [Roslyn](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/).


The algorithm that the project uses is inspired by next papers:
* [Inducing compact but accurate treesubstitution grammars](https://www.aclweb.org/anthology/N09-1062.pdf)
* [Type-based MCMC](https://www.aclweb.org/anthology/N10-1082.pdf)
* [Mining idioms from source code](https://arxiv.org/pdf/1404.0417.pdf)

An overview of the theory and the implementation: Automating API-based Generators Development by Inferring Code Idioms using Machine-Learning

## Two implementations

The repository currently contains two similar implementations of the algorithm.
The reason for this is that the algorithm includes factorial and rising factorial operations. Long and double-precision numbers cannot hold all the values resulting from these operations. To support these operations, the first implementation, available on the master branch, uses BigInteger class. The second operation relies on a customized implementation of the BigRational for better precision and is available on the BigRational branch. 

## Running these solutions
As a test data set, you can use a corpus of student files available on [Mendeley Data](http://dx.doi.org/10.17632/rbvz68v555.1).

Both versions can be compiled and run on the Windows platform using Visual Studio.
### Runing the BigInteger version
* Download the data set
* Cloning the solution: git clone https://github.com/lukic-aleksandar/RoseLibML.git
* Building the solution: 
	* Open the .sln file using Visual Studio
	 * Build -> Build Solution
* Change paths in Program.cs
	* At line 23, provide an input path to the data set as a first parameter, and an output path for the model as a second
	 * At line 33, provide a path to a file where the resulting code idioms will be printed
* Configure Training parameters:
	 * Program.cs: Total number of iterations, burn-in iterations and a threshold can be set at line 37 
	 * Core/TBSampler.cs: Alpha value and CutProbability values can be changed
 * Start!


### Runing the BigRational version
* Download the data set
* Cloning the solution: 
	* Open cmd/terminal/PowerShell
	* git clone https://github.com/lukic-aleksandar/RoseLibML.git
	* cd .\RoseLibML\
	* git checkout BigRational
* Building the solution: 
	* Open the .sln file using Visual Studio
	 * Build -> Build Solution
* Configure Training parameters:
	 * Program.cs: Total number of iterations, burn-in iterations and a threshold can be set at line 58 
	 * Core/TBSampler.cs: Alpha value and CutProbability values can be changed
 * Change the Command line arguments to configure paths:
	 * Solution Explorer  -> right-click on the RoseLibML project -> Properties
	 * Debug -> Start options -> Command line arguments
	 * Next parameters should be added:
		 * An input path to the data set
		 * An output path for the model
		 * A path to a file where the resulting code idioms will be printed
		Example: "C:\dataset" "C:\output" "C:\output\idioms.txt" 
 * Start!


