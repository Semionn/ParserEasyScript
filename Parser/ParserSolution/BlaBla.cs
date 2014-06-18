using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace ParserNamespace
{
    class DebugClass
    {
        void T(string s)
        {
            ProgramParse ps = new ProgramParse();
            ps.Parse(s);
            string str1 = ps.statements.ToString();
           
            ps.Run();
            //Console.WriteLine(str1);
            Console.WriteLine(ps.output);
        }
        void CheckParsing(string s)
        {
            ProgramParse ps = new ProgramParse();
            ps.Parse(s);
            string str1 = ps.statements.ToString();
            ProgramParse ps2 = new ProgramParse();
            ps2.Parse(str1);
            string str2 = ps2.statements.ToString();
            ProgramParse ps3 = new ProgramParse();
            ps3.Parse(str2);
            string str3 = ps2.statements.ToString();
            if (str2 != str3)
            {
                Console.WriteLine(str2);
                Console.WriteLine(str3);
                throw new Exception();
            }
            ps3.Run();
            Console.WriteLine();
            //Console.WriteLine(ps.output);
        }
        public void GG()
        {

            /*T(@"
GG(2+t);
");*/
            if (false) T(@"
 t = 1;
f = function(a){
 t = 2;
 print(t*3+a*t*2-1);
 return a+1;
 };
 print(1,2,3);
"); 
           // T(@"a(b());");

            if (false) T(@"

a = 1; 
pr = function(b){ print(a + b); }; 
pr(2);
");

            if (true) T(@"
sum = function(a, b){ return a + b; };
print(sum(1, 2));
make_null = function(){};
print(make_null());
gcd = function(a, b){
while (b != 0){
r = a % b;
a = b;
b = r;
}
return a;
};
gcd2 = function(a, b) {
 if (b == 0) {
 return a;
 }
 return gcd2(b, a % b);
};
i = 0;
while (i < 10){
j = 0;
while (j < 10){
print(i,j,gcd(i,j),gcd2(i, j));
j = j + 1;
}
i = i + 1;
}
");
          return;
          var i = 0;
          while (i < 10)
          {
              var j = 0;
              while (j < 10)
              {
                  Console.WriteLine(gcd2(i, j));
                  j = j + 1;
              }
              i = i + 1;
          }

        }

        int gcd2(int a, int b)
        {
            if (b == 0)
            {
                return a;
            }
            return gcd2(b, a % b);
        }
    }
}
