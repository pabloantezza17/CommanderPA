using System;

namespace TestRunner
{
    public class TestView
    {
        public Boolean IsSelected { get; set; }

        public String Name { get; set; }

        public String Description
        {
            get
            {
                String name = this.Name.Replace(Settings.Default.ProjectPath, String.Empty);
                return name.Replace(@"bin\", String.Empty);
            }
        }
    }
}