using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;

namespace InputRequiredExample.Data
{
    public class Context : DbContext
    {
        public Context()
            : base("InputRequired")
        {
            //Evil hack because I didn't feel like adding migrations + seed method.  Never do this in DbContext ctor
            if (!RequiredInputTypes.Any(t => t.InputName == "6*7"))
                RequiredInputTypes.Add(new RequiredInputType { InputName = "6*7", Description = "The Question" });
            if (!RequiredInputTypes.Any(t => t.InputName == "Row 7 Input"))
                RequiredInputTypes.Add(new RequiredInputType { InputName = "Row 7 Input", Description = "Row 7 is important" });
        }
        public DbSet<LongRunningJob> LongRunningJobs { get; set; }
        public DbSet<ResumableJobState> ResumableJobStates { get; set; }
        public DbSet<RequiredInputType> RequiredInputTypes { get; set; }
    }

    public class RequiredInputType
    {
        public int Id { get; set; }
        public string InputName { get; set; }//Just a handy human readable name
        public string Description { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        public string StringValue { get; set; }
        public float? FloatValue { get; set; }
    }

    public class LongRunningJob
    {
        public int Id { get; set; }
        public string UserId { get; set; }//Who owns this 
        public string State { get; set; }//Just string enum to keep it simple
        public int ResumableJobStateId { get; set; }
        public ResumableJobState ResumableJobState { get; set; }//TODO, you could have a type serialized to string instead of a database table
    }

    public class ResumableJobState
    {
        public int Id { get; set; }
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public int CurrentIteration { get; set; }
        //Or whatever state you need to resume reading a file where you left off.  Maybe a line number or row number.
        public Int64 InputFileBufferReadPosition { get; set; }
        public string Feedback { get; set; }
        public int? IntInput { get; set; }
        public bool? BoolInput { get; set; }
        public float? FloatInput { get; set; }
        public string StringInput { get; set; }
        public int? RequiredInputTypeId { get; set; }
        public RequiredInputType RequiredInputType { get; set; }
    }
}