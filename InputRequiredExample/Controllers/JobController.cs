using InputRequiredExample.Data;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace InputRequiredExample.Controllers
{
    public class JobController : Controller
    {
        private Context _context;
        private JobinatorService _jobinatorService;
        public JobController()
        {
            _context = new Context();
            _jobinatorService = new JobinatorService(_context);
        }

        public ActionResult Index()
        {
            ViewBag.ActiveJobs = _context.LongRunningJobs.Where(t => t.State != "Completed").ToList();//TODO, filter by logged in User
            return View();
        }

        [HttpPost]
        public JsonResult StartJob(string filename)//or maybe you've already uploaded and have a fileId instead
        {
            var jobState = new ResumableJobState
            {
                CurrentIteration = 0,
                InputFile = filename,
                OutputFile = filename + "_output.csv"
            };

            var job = new LongRunningJob
            {
                State = "Running",
                ResumableJobState = jobState
            };

            _context.ResumableJobStates.Add(jobState);
            _context.LongRunningJobs.Add(job);
            var result = _context.SaveChanges();
            if (result == 0) throw new Exception("Error saving to database");

            _jobinatorService.StartOrResume(job);

            return Json(job);
        }

        [HttpGet]
        public JsonResult GetJobState(int jobId)
        {
            var job = _context.LongRunningJobs.Include("ResumableJobState.RequiredInputType").FirstOrDefault(t => t.Id == jobId);
            if (job == null)
                throw new HttpException(404, "No job found with that Id");
            return Json(job, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostInput(int jobId, RequiredInputType userInput)
        {
            if (!ModelState.IsValid)
                throw new HttpException(500, "Bad input");

            var job = _context.LongRunningJobs.Include("ResumableJobState.RequiredInputType").FirstOrDefault(t => t.Id == jobId);
            job.ResumableJobState.BoolInput = userInput.BoolValue;
            job.ResumableJobState.IntInput = userInput.IntValue;
            job.ResumableJobState.FloatInput = userInput.FloatValue;
            job.ResumableJobState.StringInput = userInput.StringValue;
            _context.SaveChanges();

            if (job == null)
                throw new HttpException(404, "No job found with that Id");

            if (userInput.InputName == job.ResumableJobState.RequiredInputType.InputName)//Do some checks to see if they provided input matching the requirements
                _jobinatorService.StartOrResume(job);
            //TODO have the jobinator return the State after it's resumed, otherwise we need another Get to check the state. 
            return Json(job);
        }

        /// <summary>
        /// Stuff this in it's own service.  This way, you could use it in other places; for example starting scheduled jobs from a cron job
        /// </summary>
        public class JobinatorService//Ideally use Dependency Injection, or something good practicey to get an instance of this
        {
            private Context _context = new Context();
            private string _filePath = "";
            public JobinatorService(Context context)
            {
                _context = context;
                _filePath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString() + "/";
            }

            public void StartOrResume(LongRunningJob job)
            {
                Task.Run(() =>
                {
                    using (var inputFile = System.IO.File.OpenRead(_filePath + job.ResumableJobState.InputFile))
                    using (var outputFile = System.IO.File.OpenWrite(_filePath + job.ResumableJobState.OutputFile))
                    {
                        inputFile.Position = job.ResumableJobState.CurrentIteration;
                        for (int i = (int)inputFile.Position; i < inputFile.Length; i++)//casting long to int, what could possibly go wrong?
                        {

                            if (job.State == "Input Required" && job.ResumableJobState.RequiredInputType != null)
                            {//We needed input and received it
                                //You might want to do a switch..case on the various inputs, and branch into different functions

                                if (job.ResumableJobState.RequiredInputType.InputName == "6*7")
                                    if (job.ResumableJobState.RequiredInputType.IntValue.Value == 42)
                                        break;//Pass Go, collect 42 dollars;
                            }
                            outputFile.WriteByte((byte)inputFile.ReadByte());//Don't try this at home!

                            job.ResumableJobState.CurrentIteration = i;//or row, or line, or however you delimit processing
                            job.ResumableJobState.InputFileBufferReadPosition = inputFile.Position;//or something

                            if (i % 7 == 0)
                                job.ResumableJobState.RequiredInputType = _context.RequiredInputTypes.First(t => t.InputName == "Row 7 Input");
                            if (i % 42 == 0)
                                job.ResumableJobState.RequiredInputType = _context.RequiredInputTypes.First(t => t.InputName == "6*7");

                            if (job.ResumableJobState.RequiredInputType != null)
                                job.State = "Input Required";
                            _context.SaveChanges();
                            if (job.State != "Running")
                                return;
                        }
                        job.State = "Completed";
                        _context.SaveChanges();
                    }
                });
                return;
            }
        }
    }
}
