# HyperLogLog
the fast hyperloglog implement for .net 
is optimize from https://github.com/Microsoft/CardinalityEstimation

## Usage
Usage is very simple:
```
IHyperLogLog<string> estimator = new FastHyperLogLog();

estimator.Add("Alice");
estimator.Add("Bob");
estimator.Add("Alice");
estimator.Add("George Michael");

ulong count = estimator.Count(); // will be 3


IHyperLogLog<int> estimator = new FastHyperLogLog();

estimator.Add(1);
estimator.Add(2);
estimator.Add(3);
estimator.Add(2);

ulong count = estimator.Count(); // will be 3


uint[] array = new uint[] {1,2,3,2 };

IHyperLogLog<uint> estimator = new FastHyperLogLog();
estimator.BulkAdd(array);

ulong count = estimator.Count(); // will be 3
```

## Nuget Package
This code is available as the Nuget package [`HyperLogLog`](https://www.nuget.org/packages/HyperLogLog/).  To install, run the following command in the Package Manager Console:

```
Install-Package HyperLogLog

```
### Performance of over 10000000 iterations - typical usage

<table>
	<tr>
		<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>IHyperLogLog\<int\></td>
		<td>81ms</td>
		<td></td>
	</tr>
	<tr>
		<td>IHyperLogLog<uint></td>
		<td>118ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog<long></td>
		<td>559ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog<ulong></td>
		<td>859ms</td>
		<td>&nbsp;</td>
	</tr>
</table>
