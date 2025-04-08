﻿using Tutorial3.Models;

public class EmpDeptSalgradeTests
{
    // 1. Simple WHERE filter
    // SQL: SELECT * FROM Emp WHERE Job = 'SALESMAN';
    [Fact]
    public void ShouldReturnAllSalesmen()
    {
        var emps = Database.GetEmps();

        List<Emp> result = emps.Select(e => e).Where(e => e.Job == "SALESMAN").ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("SALESMAN", e.Job));
    }

    // 2. WHERE + OrderBy
    // SQL: SELECT * FROM Emp WHERE DeptNo = 30 ORDER BY Sal DESC;
    [Fact]
    public void ShouldReturnDept30EmpsOrderedBySalaryDesc()
    {
        var emps = Database.GetEmps();

        List<Emp> result = emps.Select(e => e).Where(e => e.DeptNo ==30).OrderByDescending(e=>e.Sal).ToList();
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Sal >= result[1].Sal);
    }

    // 3. Subquery using LINQ (IN clause)
    // SQL: SELECT * FROM Emp WHERE DeptNo IN (SELECT DeptNo FROM Dept WHERE Loc = 'CHICAGO');
    [Fact]
    public void ShouldReturnEmployeesFromChicago()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();

        List<Emp> result = emps.Where(e => depts.Where(d=> d.Loc=="CHICAGO")
            .Select(d=>d.DeptNo).Contains(e.DeptNo)).ToList(); 

        Assert.All(result, e => Assert.Equal(30, e.DeptNo));
    }

    // 4. SELECT projection
    // SQL: SELECT EName, Sal FROM Emp;
    [Fact]
    public void ShouldSelectNamesAndSalaries()
    {
        var emps = Database.GetEmps();

        var result = from e in emps select new { e.EName, e.Sal };
        Assert.All(result, r =>
        { 
            Assert.False(string.IsNullOrWhiteSpace(r.EName));
             Assert.True(r.Sal > 0);
        });
    }

    // 5. JOIN Emp to Dept
    // SQL: SELECT E.EName, D.DName FROM Emp E JOIN Dept D ON E.DeptNo = D.DeptNo;
    [Fact]
    public void ShouldJoinEmployeesWithDepartments()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();

        var result = from e in emps
                    join d in depts on e.DeptNo equals d.DeptNo
                    select new
                    {
                        e.EName, d.DName
                    };

        Assert.Contains(result, r => r.DName == "SALES" && r.EName == "ALLEN");
        Assert.Contains(result, r => r.DName == "SALES" && r.EName == "WARD");
        Assert.Contains(result, r => r.DName == "RESEARCH" && r.EName == "SMITH");
        Assert.Contains(result, r => r.DName == "ACCOUNTING" && r.EName == "KING");
        Assert.Contains(result, r => r.DName == "ACCOUNTING" && r.EName == "FORD");
        Assert.Equal(5,result.Count());
    }

    // 6. Group by DeptNo
    // SQL: SELECT DeptNo, COUNT(*) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCountEmployeesPerDepartment()
    {
        var emps = Database.GetEmps();

        var result = from e in emps
            group e by e.DeptNo into g
            select new {
            DeptNo = g.Key,
            Count = g.Count(),
        };
        
        
        Assert.Contains(result, g => g.DeptNo == 30 && g.Count == 2);
    }

    // 7. SelectMany (simulate flattening)
    // SQL: SELECT EName, Comm FROM Emp WHERE Comm IS NOT NULL;
    [Fact]
    public void ShouldReturnEmployeesWithCommission()
    {
        var emps = Database.GetEmps();

        var result = emps.Where(e=>e.Comm != null)
            .SelectMany(e=> new[] {new{e.EName,e.Comm}});
        
        Assert.All(result, r => Assert.NotNull(r.Comm));
        Assert.Equal(2, result.Count());
    }

    // 8. Join with Salgrade
    // SQL: SELECT E.EName, S.Grade FROM Emp E JOIN Salgrade S ON E.Sal BETWEEN S.Losal AND S.Hisal;
    [Fact]
    public void ShouldMatchEmployeeToSalaryGrade()
    {
        var emps = Database.GetEmps();
        var grades = Database.GetSalgrades();

        var result = emps.Join
            (
                grades,
                emps => true,
                grades => true,
                (emps, grades) => new {emps, grades})
            .Where(e => e.emps.Sal >= e.grades.Losal && e.emps.Sal <= e.grades.Hisal)
            .Select(e => new { e.emps.EName, e.grades.Grade});
        
        Assert.Contains(result, r => r.EName == "ALLEN" && r.Grade == 3);
    } //+

    // 9. Aggregation (AVG)
    // SQL: SELECT DeptNo, AVG(Sal) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCalculateAverageSalaryPerDept()
    {
        var emps = Database.GetEmps();
        
        var result = emps.GroupBy(e=>e.DeptNo).Select(gr => new
        { DeptNo = gr.Key, AvgSal = gr.Average(e=>e.Sal), });
        
        Assert.Contains(result, r => r.DeptNo == 30 && r.AvgSal > 1000);
        Assert.Equal(3, result.Count());
    }

    // 10. Complex filter with subquery and join
    // SQL: SELECT E.EName FROM Emp E WHERE E.Sal > (SELECT AVG(Sal) FROM Emp WHERE DeptNo = E.DeptNo);
    [Fact]
    public void ShouldReturnEmployeesEarningMoreThanDeptAverage()
    {
        var emps = Database.GetEmps();
            
         var result = from e1 in emps
             where e1.Sal > (from e2 in emps where e2.DeptNo==e1.DeptNo select e2.Sal).Average()
                 select e1.EName;
        
        Assert.Contains("ALLEN", result);
    }
}
