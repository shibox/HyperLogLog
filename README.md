# HyperLogLog
the fastest hyperloglog implement for .net 

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
### Performance of over 10,000,000 iterations -single typical usage

<table>
	<tr>
		<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;int&gt;</td>
		<td>314ms</td>
		<td></td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;uint&gt;</td>
		<td>314ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;long&gt;</td>
		<td>315ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;ulong&gt;</td>
		<td>316ms</td>
		<td>&nbsp;</td>
	</tr>
</table>


### Performance of over 10,000,000 iterations -bulk typical usage

<table>
	<tr>
		<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;int&gt;</td>
		<td>270ms</td>
		<td></td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;uint&gt;</td>
		<td>271ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;long&gt;</td>
		<td>272ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>IHyperLogLog&lt;ulong&gt;</td>
		<td>272ms</td>
		<td>&nbsp;</td>
	</tr>
</table>
