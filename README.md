
# RoseLibML

## The project

This project aims to enable inference of code idioms from an unlabeled data set.
The architecture of the solution is modular. The inference core can be extended to support an arbitrary language.
Currently, the solution supports inference from files written in C# language, using [Roslyn](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/).


The algorithm that the project uses is inspired by next papers:
* [Inducing compact but accurate treesubstitution grammars](https://www.aclweb.org/anthology/N09-1062.pdf)
* [Type-based MCMC](https://www.aclweb.org/anthology/N10-1082.pdf)
* [Mining idioms from source code](https://arxiv.org/pdf/1404.0417.pdf)


## Running the solution
As a test data set, you can use a corpus of student files available on [Mendeley Data](http://dx.doi.org/10.17632/rbvz68v555.1).

The solution is built using .NET 6.0, it should be able to run Windows, Linux, and MacOS. 
So far, it has only been tested on the Windows.

No executable available at this moment. It will be available once there is a stable version.

## Compiling the solution
The solutions can be compiled using Visual Studio.

* Building the solution: 
	* Open the .sln file using Visual Studio
	* Build -> Build Solution

The compiled version should now be available under the bin folder of the solution, in Debug or Release folders.

To run the solution using Visual Studio, the path to config.js have to be set using the Command line arguments:
-   Solution Explorer -> right-click on the RoseLibML project -> Properties
-   Debug -> Start options -> Command line arguments

Be sure that the config.json file is available and that it contains the right values.
 
