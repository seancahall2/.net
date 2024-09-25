using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PersonnelApi.Jobs;
using System;
using System.Diagnostics;

namespace PersonnelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly ILogger<TestJob> _logger;

        public JobController(ILogger<TestJob> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("CreateBackgroundJob")]
        public ActionResult CreateBackgroundJob()
        {
            BackgroundJob.Enqueue<TestJob>(x => x.WriteLog("Background Job Triggered"));
            return Ok();
        }

        // this is a one time job
        [HttpPost]
        [Route("CreateScheduledJob")]
        public ActionResult CreateScheduledJob()
        {
            var scheduleDateTime = DateTime.UtcNow.AddSeconds(5);
            var dateTimeOffset = new DateTimeOffset(scheduleDateTime);
            BackgroundJob.Schedule<WorkdayJob>(x => x.RunSecondImport("Identity Import Job Triggered"), dateTimeOffset);
            //  RecurringJob.AddOrUpdate(() => Debug.WriteLine("Scheduled Job Triggered"), Cron.Minutely);
            return Ok();
        }

        // this is a continuation job that will run after the first job id is run.
        [HttpPost]
        [Route("CreateContinuationJob")]
        public ActionResult CreateContinuationJob()
        {
            var scheduleDateTime = DateTime.UtcNow.AddSeconds(300);
            var dateTimeOffset = new DateTimeOffset(scheduleDateTime);
            var parentId = BackgroundJob.Schedule<TestJob>(x => x.WriteLog("Parent Job Triggered"), dateTimeOffset);
            var jobId2 = BackgroundJob.ContinueJobWith<TestJob>(parentId, x => x.WriteLog("Continuation Job2 Triggered"));

            BackgroundJob.ContinueJobWith<TestJob>(parentId, x => x.WriteLog("Continuation Job Triggered"));
            return Ok();
        }

        // recurring job, this is our target method
        [HttpPost]
        [Route("CreateRecurringJob")]
        public ActionResult CreateRecurringJob()
        {
            RecurringJob.AddOrUpdate<WorkdayJob>(x => x.RunImport("Workday Import Job Triggered"), "0 5 * * *");
            RecurringJob.AddOrUpdate<WorkdayJob>(x => x.RunSecondImport("Workday 2nd Import Job Triggered"), "0 6 * * *");
            return Ok();
        }

    }
}
