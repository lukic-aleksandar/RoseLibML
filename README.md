
# RoseLibML

## The project

This project aims to enable inference of code idioms from an unlabeled data set.
The architecture of the solution is modular. The inference core can be extended to support an arbitrary language.
Currently, the solution supports inference from files written in C# language, using [Roslyn](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/).


The algorithm that the project uses is inspired by next papers:
* [Inducing compact but accurate treesubstitution grammars](https://www.aclweb.org/anthology/N09-1062.pdf)
* [Type-based MCMC](https://www.aclweb.org/anthology/N10-1082.pdf)
* [Mining idioms from source code](https://arxiv.org/pdf/1404.0417.pdf)

## Two implementations

The repository currently contains two similar implementations of the algorithm.
The reason for this is that the algorithm includes factorial and rising factorial operations. Long and double-precision numbers cannot hold all the values resulting from these operations. 

To support these operations, the first implementation uses BigInteger class. This is the implementation available on the main branch.
The second implementation relies on a custom BigRational for better precision. This implementation is available on the BigRational branch and is currently under development.

## Running these solutions
As a test data set, you can use a corpus of student files available on [Mendeley Data](http://dx.doi.org/10.17632/rbvz68v555.1).

Both these solutions can be run on the Windows platform. Compiled executables are available in the 

### Runing the BigInteger version
* Download the data set
* Open cmd/PowerShell and locate to desired folder
* Clone the solution: git clone https://github.com/lukic-aleksandar/RoseLibML.git
* Extract Compiled.zip found in the cloned folder
* Enter the resulting Compiled folder, open config.json
* In config.json Change the parameters; change the path to the dataset; set the output path for idioms
* Using cmd/PowerShell enter the RoseLibML folder found inside the Compiled folder
* Run using  cmd/PowerShell: .\RoseLibML.exe "C:\example-path\config.json"


### Runing the BigRational version
* Download the data set
* Open cmd/PowerShell and locate to desired folder
* Clone the solution: 
	* git clone https://github.com/lukic-aleksandar/RoseLibML.git
	* cd .\RoseLibML\
	* git checkout BigRational
* Extract Compiled.zip found in the cloned folder
* Enter the resulting Compiled folder, open config.json
* In config.json Change the parameters; change the path to the dataset; set the output path for idioms
* Using cmd/PowerShell enter the RoseLibML folder found inside the Compiled folder
* Run using  cmd/PowerShell: .\RoseLibML.exe "C:\example-path\config.json"



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
 
