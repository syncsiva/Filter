﻿using PerftTest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PerfTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Program();
            Console.WriteLine("Hello World!");
        }

        public List<Order> Orders { get; set; }
        public List<EmployeeData> Employees { get; set; }
        public string Timetaken { get; set; }

        public Program()
        {
            Orders = Enumerable.Range(1, 1000).Select(x => new Order()
            {
                OrderID = 1000 + x,
                EmployeeID = x,
                Freight = 2.1 * x,
                OrderDate = DateTime.Now.AddDays(-x),
            }).ToList();

            Employees = Enumerable.Range(1, 1000).Select(x => new EmployeeData()
            {
                EmployeeID = x,
                FirstName = (new string[] { "Nancy", "Andrew", "Janet", "Margaret", "Steven" })[new Random().Next(5)],
            }).ToList();


            var field = "EmployeeID";
            var PredicateList = new List<Filtering.WhereFilter>();
            var query = new List<Filtering.WhereFilter>();
            foreach (var rec in Orders as IEnumerable<object>)
            {
                var nameOfProperty = "EmployeeID";
                var propertyInfo = rec.GetType().GetProperty(nameOfProperty);
                var key = propertyInfo.GetValue(rec, null);
                {
                    PredicateList.Add(new Filtering.WhereFilter()
                    {
                        Field = field,
                        value = key,
                        IgnoreCase = false,
                        Operator = "equal"
                    });
                }
            }

            query.Add(new Filtering.WhereFilter() { Condition = "or", IsComplex = true, predicates = PredicateList });
            var datetime = DateTime.Now;
            var final = Filtering.PerformFiltering(Employees as IEnumerable<object>, query, "and");
            var result = final.ToList();
            var data = result;
            Timetaken = "Time taken " + (DateTime.Now - datetime).TotalMilliseconds + " milliseconds";
            Console.WriteLine(Timetaken);
        }
    }


    public class Order
    {
        public int? OrderID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime? OrderDate { get; set; }
        public double? Freight { get; set; }
    }

    public class EmployeeData
    {
        public int? EmployeeID { get; set; }
        public string FirstName { get; set; }
    }
}
