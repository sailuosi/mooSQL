// 基础功能说明：

using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMooSQL.src;

public class TestEnurableUntils
{


    [Fact]
    public void sumList() { 
    
        var list1= new List<int>() { 1,5,9,15,55,4655,11};

        var res = list1.reduece<int,int>((x1,x2) => x1+x2);

        Assert.Equal(1,res);
    }

    [Fact]
    public void sumList1()
    {

        var list1 = new List<propoRow>() {
            new propoRow{ 
                id=0,
                name="11"
            }
        };

        var res = list1.sum((x) => x.idx);
        var price = list1.sum((x) => x.price);
        var score = list1.sum((x) => x.score);

        Assert.Equal(1, res);
    }
}

public class propoRow {
    public int id;
    public int? idx;
    public string name;
    public string description;
    public double price;
    public decimal score;
}