using System;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;

namespace PaySlipGeneratorPOC
{
    class Program
    {        
        static string taxfilepath = string.Empty;
        static string inputfilepath = string.Empty;
        static string outputFilePath = string.Empty;
        static DataTable taxslab = null;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter Path for Tax File path");
            taxfilepath = Console.ReadLine();
            Console.WriteLine("Enter Path for Employee Input file path");
            inputfilepath = Console.ReadLine();
            Console.WriteLine("Enter Path for Employee Output file path");
            outputFilePath = Console.ReadLine();

            taxslab = GetTaxSlabs(taxfilepath); // To read the taxSlab csv file
            DataTable inputdata = ConvertCSVtoDataTable(inputfilepath); //To read the Employee Input details csv file
            DataTable outputdata= ProcessSalarySlip(inputdata); //To Process the Employee Output data
            OutputCSVGenerator(outputdata, outputFilePath); //To export the EmployeeOutput csv
        }

        public static DataTable GetTaxSlabs(string taxfilepath)
        {
            StreamReader sr = new StreamReader(taxfilepath);

            //Tax Slab structure
            DataTable taxmaster = new DataTable();
            taxmaster.Columns.Add("Taxable income Min", typeof(string));
            taxmaster.Columns.Add("Taxable income maximum", typeof(string));
            taxmaster.Columns.Add("Basic Tax", typeof(string));
            taxmaster.Columns.Add("Tax on this income", typeof(string));            

            while (!sr.EndOfStream)
            {
                string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                DataRow dr = taxmaster.NewRow();
                for (int i = 0; i < rows.Length; i++)
                {
                    dr[i] = rows[i];
                }
                taxmaster.Rows.Add(dr);
            }
            return taxmaster;

        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            StreamReader sr = new StreamReader(strFilePath);            
            DataTable inputdata = new DataTable();
            inputdata.Columns.Add("FirstName", typeof(string));
            inputdata.Columns.Add("LastName", typeof(string));
            inputdata.Columns.Add("AnnualSalary", typeof(int));
            inputdata.Columns.Add("SuperRate", typeof(string));
            inputdata.Columns.Add("MonthStartDate", typeof(string));

            while (!sr.EndOfStream)
            {
                string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                DataRow dr = inputdata.NewRow();
                for (int i = 0; i < rows.Length; i++)
                {
                    dr[i] = rows[i];
                }
                inputdata.Rows.Add(dr);
            }
            return inputdata;

        }

        public static DataTable ProcessSalarySlip(DataTable res)
        {
            DataTable outputdata = new DataTable();
            //columns  
            outputdata.Columns.Add("Name", typeof(string));
            outputdata.Columns.Add("Pay_Period", typeof(string));
            outputdata.Columns.Add("GrossIncome", typeof(int));
            outputdata.Columns.Add("IncomeTax", typeof(int));
            outputdata.Columns.Add("NetIncome", typeof(double));
            outputdata.Columns.Add("Super", typeof(int));
            DataRow dr = null;

            foreach (DataRow row in res.Rows)
            {
                dr = outputdata.NewRow();
                string name = row[0].ToString();
                string fullname = name + " " + row[1].ToString();
                dr["Name"] = fullname;

                //processing date range
                string dd= row[4].ToString().Substring(0,2);
                string MM = row[4].ToString().Substring(2,2);
                string yyyy = row[4].ToString().Substring(4,4);
                string MonthStartDate = dd +"/"+ MM +"/"+ yyyy;
                DateTime dt = DateTime.ParseExact(MonthStartDate, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime endDt = dt.AddMonths(1).AddDays(-1);
                string monthstart = dt.ToString("dd/MMMM/yyyy");
                string monthend = endDt.ToString("dd/MMMM/yyyy");
                string Pay_Period = monthstart.ToString() + " - " + monthend.ToString();
                //end processing date range

                dr["Pay_Period"] = Pay_Period.ToString();

                int grossincome = Convert.ToInt32(row[2].ToString()) / 12;
                dr["GrossIncome"] = grossincome;
                
                Decimal tax = IncomeTaxcalculator(row[2].ToString());
                Decimal incometax =  Math.Ceiling(tax);
                dr["IncomeTax"] = incometax;

                Decimal netincome = grossincome - incometax;
                dr["NetIncome"] = netincome;
                
                int super = (grossincome * Convert.ToInt32(row[3].ToString().Replace("%", "")))/ 100;
                dr["Super"] = super;

                outputdata.Rows.Add(dr);
            }
            return outputdata;
        }

        public static void OutputCSVGenerator (DataTable dt,string strFilePath)
        {
            StringBuilder sb = new StringBuilder();            
            foreach (DataRow dr in dt.Rows)
            {
                foreach (DataColumn dc in dt.Columns)
                    sb.Append(WriteCSV(dr[dc.ColumnName].ToString()) + ",");
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
            }
            File.WriteAllText(strFilePath, sb.ToString());
        }

        public static string WriteCSV(string input)
        {
            try
            {
                if (input == null)
                    return string.Empty;
                else
                    return input;
            }
            catch
            {
                throw;
            }
        }

        public static Decimal IncomeTaxcalculator(string annualsalary)
        {
            decimal incometax = 0;
                foreach(DataRow column in taxslab.Rows)
                {
                    if (Convert.ToInt32(annualsalary) > Convert.ToInt32(column[0].ToString()) && Convert.ToInt32(annualsalary) < Convert.ToInt32(column[1].ToString()))
                    {
                        decimal tax = Convert.ToInt32(column[2].ToString()) + (((Convert.ToInt32(annualsalary) - Convert.ToInt32(column[0].ToString())+1)* (Convert.ToDecimal(column[3].ToString())/100)));
                        incometax = Convert.ToDecimal(tax) /12;
                    }
                }
            return incometax;
        }

    }
}
