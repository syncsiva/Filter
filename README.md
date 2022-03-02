# Queryable.Where causing performance issue in .NET 6

Find time taken for same code between .NET5 and .NET6 in ASP.NET Core Blazor App.

**.NET 5**
Time taken 210.2811 milliseconds

**.NET 6**
Time taken 79274.2204 milliseconds 

## Replication Steps using simple console app
1. Open solution or proj file under `Console App` folder.
2. Run the app where you can see performance in console.
3. Now change the target framework in project file and see difference between .NET 5 and .NET 6.


## Replication steps in Blazor App

1. Open `LambdaPerformance_NET6.sln` for running .NET 6 Blazor App and `LambdaPerformance_NET5.sln` for running .NET 5 Blazor App.
2. Place break appoint in `Filter` method of `~Pages\Index.razor` file.

![image](https://user-images.githubusercontent.com/18731572/156349966-c9d41922-b3ac-4fc3-b5f7-06047df49f99.png)

3. Run the app and check value in `Timetaken`. 
