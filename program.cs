//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WpfTest
{
    using System;
    using System.Collections.Generic;
    
    public partial class program
    {
        public program()
        {
            this.job_demand = new HashSet<job_demand>();
            this.job_requirements = new HashSet<job_requirements>();
            this.university_programs = new HashSet<university_programs>();
        }
    
        public long id { get; set; }
        public string name { get; set; }
        public long parent { get; set; }
    
        public virtual ICollection<job_demand> job_demand { get; set; }
        public virtual ICollection<job_requirements> job_requirements { get; set; }
        public virtual ICollection<university_programs> university_programs { get; set; }
    }
}
