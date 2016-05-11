# HyperLogLog
the fast hyperloglog implement for .net 
is optimize from https://github.com/Microsoft/CardinalityEstimation

## Usage
Usage is very simple:
```
ICardinalityEstimator<string> estimator = new CardinalityEstimator();

estimator.Add("Alice");
estimator.Add("Bob");
estimator.Add("Alice");
estimator.Add("George Michael");

ulong numberOfuniqueElements = estimator.Count(); // will be 3
```

## Nuget Package
This code is available as the Nuget package [`HyperLogLog`](https://www.nuget.org/packages/HyperLogLog/).  To install, run the following command in the Package Manager Console:
```
Install-Package HyperLogLog
