using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PersonnelApi.DTOs;
using System.Net.Http.Headers;
using System.Net;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using static PersonnelApi.DTOs.WorkdayPerson.Employmentdetail;
using PersonnelApi.Models;
using static PersonnelApi.DTOs.WorkdayPerson;
using Microsoft.Extensions.Logging.Abstractions;
using PersonnelApi.Services;
using Org.BouncyCastle.Asn1.Pkcs;

namespace PersonnelApi.Jobs
{
    public class WorkdayJob : ControllerBase
    {
        private readonly ILogger<WorkdayJob> _logger;
        private readonly IMailService _mailService;
        public XXXSchoolContext _context;

        public WorkdayJob(XXXSchoolContext context, ILogger<WorkdayJob> logger, IMailService _MailService)
        {
            _context = context;
            _logger = logger;
            _mailService = _MailService;
        }

        // This method is called by the JobController to run the first import against the
        public async Task RunSecondImport(string logMessage)
        {
            // Read most recent adds to personnel table that have creationuser = WorkdayImport and creationdate >= today
            DateTime today = DateTime.Today; // read once, avoid "odd" errors once in a blue moon
            DateTime yesterday = today.AddDays(-1);

            // Specify the subject name of the certificate to retrieve from the store
            string certificateSubjectName = "employee-management-dev.XXX.washington.edu";

            // Find the certificate in the local machine store by subject name
            X509Certificate2 clientCertificate = FindCertificateBySubjectName(StoreLocation.LocalMachine, certificateSubjectName);

            if (clientCertificate == null)
            {
                Debug.WriteLine($"Certificate with subject name '{certificateSubjectName}' not found in the local machine store.");
            }

            var firstImports = await _context.PersonnelAudit
               .Where(e => e.MaintenanceDate >= yesterday && e.MaintenanceUser == "WorkdayImport")
               .ToListAsync();

            // If there are records, loop through them polling the identity api for each UwnetId and update the record with the returned data
            if (firstImports.Count > 0)
            {
                foreach (var identity in firstImports)
                {
                    // Create an HttpClientHandler and configure it with the client certificate
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ClientCertificates.Add(clientCertificate);

                    // Create an instance of HttpClient with the configured handler
                    HttpClient client = new HttpClient(handler);

                    var uwnetid = identity.UwnetId;
                    var verbose = "true";
                    var format = "json";
                    var apiUrl = "https://xxx.x.xx.xx/xxxxxxx/v2/person";

                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Construct the API URL with the query parameters for this person
                    string APIUrl = string.Format(apiUrl + "?uwnetid={0}&format={1}&verbose={2}", uwnetid, format, verbose);

                    try
                    {
                        // Make an HTTP GET request to the API endpoint
                        HttpResponseMessage response = await client.GetAsync(APIUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            Debug.WriteLine($"Response: {responseBody}");

                            // deserialize the JSON response into a C# object
                            var identityData = JsonSerializer.Deserialize<WorkdayIdentity.Rootobject>(responseBody);
                            var totalCount = identityData.TotalCount;

                            if (identityData.Persons.Length > 0)
                                Debug.WriteLine($"persons length: {identityData.Persons.Length}");
                            {
                                // Make sure to check if workdayData.Persons is not null before accessing its length
                                if (identityData.Persons != null)
                                {
                                    var result = await _context.Personnel.FirstOrDefaultAsync(e => e.UwnetId == uwnetid);
                                    if (result != null)

                                    {
                                        result.PersonPronouns = identityData.Persons[0].Pronouns;
                                        result.MiddleName = identityData.Persons[0].PreferredMiddleName;
                                        result.PreferredName = identityData.Persons[0].DisplayName;
                                        if (identityData.Persons[0].PreferredFirstName != null)
                                        {
                                            result.FirstName = identityData.Persons[0].PreferredFirstName;
                                        }
                                        if (identityData.Persons[0].PreferredSurname != null)
                                        {
                                            result.LastName = identityData.Persons[0].PreferredSurname;
                                        }
                                        result.MaintenanceUser = "IdentityImport";
                                        result.MaintenanceApp = "PersonnelDB-API";
                                        result.MaintenanceDate = DateTime.Now;

                                        await _context.SaveChangesAsync();

                                    }

                                }

                            }
                            if (identityData.Persons.Length != 0)
                            {
                                Debug.WriteLine($"Workday records found: {identityData.Persons.Length}");

                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                            _logger.LogInformation($"{today.ToString()} {logMessage} Failed to fetch data for UwNetID: {uwnetid}. Status code: {response.StatusCode}");

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"An error occurred: {ex}");
                        _logger.LogInformation($"{today.ToString()} {logMessage} Error fetching data. Exception thrown: {ex}");
                    }
                    finally
                    {
                        // Dispose of the HttpClient instance
                        client.Dispose();
                        handler.Dispose();
                    }
                }
                var mailRequest = new MailRequest
                {
                    EmailToName = "UW HR XXX",
                    EmailToId = "HRXXX@uw.edu",
                    EmailSubject = "Person Data Update",
                    EmailBody = "Workday Person data has been updated in the Personnel Database."
                };
                SendMail(mailRequest);
            }
        }

        public async Task RunImport(string logMessage)
        {
            DateTime today = DateTime.Today; // read once, avoid "odd" errors once in a blue moon
            DateTime yesterday = today.AddDays(-1);

            string changed_since_date = yesterday.ToString("yyyy-MM-dd");
            string format = "json";

            // Specify the subject name of the certificate to retrieve from the store
            string certificateSubjectName = "xxxxxx-xxxxxxxx-xxx.xxx.xxxxxxxxxxx.xxx";

            // Find the certificate in the local machine store by subject name
            X509Certificate2 clientCertificate = FindCertificateBySubjectName(StoreLocation.LocalMachine, certificateSubjectName);

            if (clientCertificate == null)
            {
                Debug.WriteLine($"Certificate with subject name '{certificateSubjectName}' not found in the local machine store.");
            }

            var orgArray = new string[] {
                "XXX_XXX001","XXX_XXX001_JM_Academic","XXX_XXX002","XXX_XXX003","XXX_XXX004","XXX_XXX005","XXX_XXX006","XXX_XXX007","XXX_XXX008","XXX_XXX008_JM_Academic","XXX_XXX009","XXX_XXX010","XXX_XXX011","XXX_XXX014","XXX_XXX016","XXX_XXX019","XXX_XXX020","XXX_XXX021","XXX_XXX023","XXX_XXX025","XXX_XXX025_JM_Academic","XXX_XXX026","XXX_XXX027","XXX_XXX027_JM_Academic","XXX_XXX030","XXX_XXX032","XXX_XXX033","XXX_XXX037","XXX_XXX038_JM_Academic","XXX_XXX041","XXX_XXX042","XXX_XXX047","XXX_XXX048","XXX_XXX050","XXX_XXX051","XXX_XXX052","XXX_XXX055","XXX_XXX056","XXX_XXX062","XXX_XXX063","XXX_XXX065","XXX_XXX066","XXX_XXX067","XXX_XXX069","XXX_XXX070","XXX_XXX072","XXX_XXX074","XXX_XXX076","XXX_XXX077","XXX_XXX078","XXX_XXX080","XXX_XXX082","XXX_XXX084","XXX_XXX093","XXX_XXX094","XXX_XXX095","XXX_371826","XXX_372183","XXX_372757","XXX_373160"
            };
            foreach (var supervisory_organization in orgArray)
            {
                // Create an HttpClientHandler and configure it with the client certificate
                HttpClientHandler handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);


                // Create an instance of HttpClient with the configured handler
                HttpClient client = new HttpClient(handler);

                var apiUrl = "https://xxxxx.s.xx.xx/xxx/v3/person";
                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var page_start = 1;
                var page_size = 250;

                string APIUrl = string.Format(apiUrl + "?changed_since_date={0}&format={1}&page_start={2}&page_size={3}&supervisory_organization={4}", changed_since_date, format, page_start, page_size, supervisory_organization);
                Debug.WriteLine("APIUrl: " + APIUrl);

                try
                {
                    // Make an HTTP GET request to the API endpoint
                    HttpResponseMessage response = await client.GetAsync(APIUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Response: {responseBody}");

                        // deserialize the JSON response into a C# object
                        var workdayData = JsonSerializer.Deserialize<WorkdayPerson.Rootobject>(responseBody);
                        var totalCount = workdayData.TotalCount;
                        page_size = 250;

                        if (workdayData.Persons.Length > 0)
                        {
                            // Make sure to check if workdayData.Persons is not null before accessing its length
                            if (workdayData.Persons != null)
                            {
                                for (int x = 0; x < workdayData.Persons.Length; x++)
                                {
                                    // Access each person in the array using workdayData.Persons[i]
                                    var person = workdayData.Persons[x];
                                    Debug.WriteLine($"LastName: {workdayData.Persons[x].LastName}");
                                    Debug.WriteLine($"UwempNo: {workdayData.Persons[x].IDs[1].Value}");
                                    // Perform operations on the person object

                                    if (workdayData.Persons[x].WorkerDetails[0].EmploymentStatus.HireDate.Length > 0)
                                    {
                                        var personStartDate = DateTime.Parse(workdayData.Persons[x].WorkerDetails[0].EmploymentStatus.HireDate);
                                        var newPerson = new PersonnelDTO
                                        {
                                            FirstName = workdayData.Persons[x].FirstName,
                                            LastName = workdayData.Persons[x].LastName,
                                            UwempNo = Convert.ToInt32(workdayData.Persons[x].IDs[1].Value),
                                            UwnetId = workdayData.Persons[x].IDs[2].Value,
                                            Title = workdayData.Persons[x].WorkerDetails[0].EmploymentDetails[0].BusinessTitle,
                                            PersonStartDate = personStartDate,
                                            PersonEmail = workdayData.Persons[x].WorkerDetails[0].PersonalData.Contact.Emails[0].EmailAddress,
                                            CreationUser = "WorkdayImport",
                                            CreationApp = "PersonnelDB-API"
                                        };

                                        // Call the AddPersonAsync method in PersonService and pass it the newPerson object
                                        await AddPersonAsync(newPerson);
                                    }
                                }

                            }

                        }
                        if (workdayData.Persons.Length != 0)
                        {
                            Debug.WriteLine($"Workday records found: {workdayData.Persons.Length}");

                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                        _logger.LogInformation($"{today.ToString()} {logMessage} Failed to fetch data for supervisory org: {supervisory_organization}. Status code: {response.StatusCode}");

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred: {ex}");
                    _logger.LogInformation($"{today.ToString()} {logMessage} Error fetching data. Exception thrown: {ex}");
                }
                finally
                {
                    // Dispose of the HttpClient instance
                    client.Dispose();
                    handler.Dispose();
                }
            }
            var mailRequest = new MailRequest
            {
                EmailToName = "xx xx XXX",
                EmailToId = "xxxxx@xx.xxxx",
                EmailSubject = "Person Data Update",
                EmailBody = "Workday Person data has been updated in the Personnel Database."
            };
            SendMail(mailRequest);
        }

        // Helper method to find a certificate by subject name in the specified store location
        static X509Certificate2 FindCertificateBySubjectName(StoreLocation storeLocation, string subjectName)
        {
            X509Certificate2? foundCertificate = null;
            using (X509Store store = new X509Store(storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                // Find certificates matching the subject name
                X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                if (certificates.Count > 0)
                {
                    // Get the first matching certificate (assuming there's only one)
                    foundCertificate = certificates[0];
                }

                store.Close();
            }

            return foundCertificate;
        }

        [HttpPost]
        public async Task<bool> AddPersonAsync(PersonnelDTO personelDto)
        {
            var dupeCheck = await _context.Personnel
                .FirstOrDefaultAsync(e => e.UwnetId == personelDto.UwnetId);
            if (dupeCheck == null)
            {
                DateTime today = DateTime.Today;
                // Map the PersonDto to a Person entity
                var person = new Personnel
                {
                    FirstName = personelDto.FirstName,
                    LastName = personelDto.LastName,
                    UwempNo = personelDto.UwempNo,
                    UwnetId = personelDto.UwnetId,
                    Title = personelDto.Title,
                    PersonStartDate = personelDto.PersonStartDate,
                    PersonEmail = personelDto.PersonEmail,
                    CreationUser = personelDto.CreationUser,
                    CreationApp = personelDto.CreationApp,
                };

                // Add the person to the database
                _context.Personnel.Add(person);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"{today.ToString()} Employee added successfully: {person}");
            }
            // If the person already exists in the database, update the existing record
            else
            {
                // Map the dupeCheck as a new record in the PersonnelAudit table
                var personAudit = new PersonnelAudit
                {
                    PersonnelId = dupeCheck.PersonnelId,
                    FirstName = dupeCheck.FirstName,
                    LastName = dupeCheck.LastName,
                    MiddleName = dupeCheck.MiddleName,
                    PreferredName = dupeCheck.PreferredName,
                    UwempNo = dupeCheck.UwempNo,
                    UwnetId = dupeCheck.UwnetId,
                    Title = dupeCheck.Title,
                    PersonStartDate = dupeCheck.PersonStartDate,
                    PersonPronouns = dupeCheck.PersonPronouns,
                    PersonPhone = dupeCheck.PersonPhone,
                    PersonEmail = dupeCheck.PersonEmail,
                    MaintenanceUser = dupeCheck.MaintenanceUser,
                    MaintenanceApp = dupeCheck.MaintenanceApp,
                    MaintenanceDate = dupeCheck.MaintenanceDate
                };

                // Add the person to the database
                _context.PersonnelAudit.Add(personAudit);
                await _context.SaveChangesAsync();

                // Finally, update the existing record in the Personnel table
                DateTime currentDateTime = DateTime.Now;
                dupeCheck.LastName = personelDto.LastName;
                dupeCheck.FirstName = personelDto.FirstName;
                dupeCheck.MiddleName = personelDto.MiddleName;
                dupeCheck.UwempNo = personelDto.UwempNo;
                dupeCheck.UwnetId = personelDto.UwnetId;
                dupeCheck.Title = personelDto.Title;
                dupeCheck.PersonStartDate = personelDto.PersonStartDate;
                dupeCheck.MaintenanceDate = currentDateTime;
                dupeCheck.MaintenanceUser = "WorkdayImport";
                dupeCheck.MaintenanceApp = "PersonnelDB-API";

                await _context.SaveChangesAsync();
                Debug.WriteLine($"Person already exists in the database: {dupeCheck}");
            }
            return true;
        }


        [HttpGet("personnelbyuwnetid")]
        public async Task<ActionResult<Personnel>> GetPersonnelByUwnetId(string UwnetId)
        {
            var result = await _context.Personnel
                .FirstOrDefaultAsync(e => e.UwnetId == UwnetId);
            if (result != null)
            {

                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("SendMail")]
        public bool SendMail(MailRequest mailRequest)
        {
            return _mailService.SendMail(mailRequest);
        }
    }
}
