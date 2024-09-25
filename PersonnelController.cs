using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PersonnelApi.DTOs;
using Microsoft.Extensions.Configuration;

namespace PersonnelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonnelController : ControllerBase
    {
        public LawSchoolContext _context;
        private IConfiguration _configuration;

        public PersonnelController(LawSchoolContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // gets all personnel records sorted by last name
        [HttpGet]
        public async Task<IActionResult> GetPersonnel()
        {
            return Ok(await _context.Personnel.OrderBy(p => p.LastName).ToListAsync());
        }

        // gets all personnel records created in the last 24 hours
        [HttpGet("imports")]
        public async Task<IActionResult> GetImports()
        {
            DateTime today = DateTime.Today; // read once, avoid "odd" errors once in a blue moon
            DateTime yesterday = today.AddDays(-1);
            var result = await _context.Personnel
                .Where(e => e.CreationDate >= yesterday)
                .Select(p => new PersonnelDTO
                {
                    PersonnelId = p.PersonnelId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    MiddleName = p.MiddleName,
                    PreferredName = p.PreferredName,
                    UwempNo = p.UwempNo,
                    UwnetId = p.UwnetId,
                    Title = p.Title,
                    PersonStartDate = p.PersonStartDate,
                    PersonEndDate = p.PersonEndDate,
                    RoomNumber = p.RoomNumber,
                    PersonPhone = p.PersonPhone,
                    PersonEmail = p.PersonEmail,
                    WebDirectory = p.WebDirectory,
                    EdirectoryEntry = p.EdirectoryEntry,
                    PersonWeb = p.PersonWeb,
                    Faculty = p.Faculty,
                    Staff = p.Staff,
                    Student = p.Student,
                    CreationUser = p.CreationUser,
                    CreationApp = p.CreationApp,
                    CreationDate = p.CreationDate,
                }).OrderByDescending(x => x.CreationDate)
                .ToListAsync();

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        // gets personnel by ID
        [HttpGet("personnelbyid")]
        public async Task<ActionResult<Personnel>> GetPersonnelById(int PersonnelId)
        {
            var result = await _context.Personnel
                .FirstOrDefaultAsync(e => e.PersonnelId == PersonnelId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        // gets personnel by UWNetID
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

        // gets personnel by PersonnelStatusID and UnitID
        [HttpGet("personneldetail")]
        public async Task<ActionResult<Personnel>> GetPersonnelDetail(int PersonnelStatusID, int UnitID)
        {
            var connectionString = _configuration.GetConnectionString("DbConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "EXEC rtvPersonnelForStatusORType @PersonnelStatusID, @UnitID";
                object temp;
                if (UnitID == 0)
                {
                    temp = DBNull.Value;
                }
                else
                {
                    temp = UnitID;
                }
                var values = new { PersonnelStatusID = PersonnelStatusID, UnitID = temp };
                var results = await connection.QueryAsync<PersonnelDTO>(sql, values);
                return Ok(results.ToList());
            }
        }

        // gets personnel by PersonnelStatusID and UnitID with titles
        [HttpGet("personneltitles")]
        public async Task<ActionResult<List<PersonnelDTO>>> GetPersonnelWithTitles(int PersonnelStatusID, int UnitID)
        {
            var connectionString = _configuration.GetConnectionString("DbConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "EXEC rtvPersonnelWithTitlesForStatusORType @PersonnelStatusID, @UnitID";
                object temp;
                if (UnitID == 0)
                {
                    temp = DBNull.Value;
                }
                else
                {
                    temp = UnitID;
                }
                var values = new { PersonnelStatusID = PersonnelStatusID, UnitID = temp };
                var results = await connection.QueryAsync<PersonnelDTO, PersonnelUnitDTO, PersonnelDTO>(sql, (personnel, personnelUnit) =>
                {
                    personnel.PersonnelUnitDTOs = personnel.PersonnelUnitDTOs ?? new List<PersonnelUnitDTO>();
                    personnel.PersonnelUnitDTOs.Add(personnelUnit);
                    return personnel;
                }, values, splitOn: "PersonnelId,PersonUnitTitle");
                var personnel = results.Distinct().ToList();
                return personnel;
            }
        }

        // gets personnel by PersonnelStatusID and UnitID with titles and roles
        [HttpGet("personnelunittitles")]
        public async Task<ActionResult<IEnumerable<PersonnelDTO>>> GetPersonnelUnitTitles(int PersonnelStatusID, int UnitID)
        {
            var personnel = await (from p in _context.Personnel
                                   join pu in _context.PersonnelUnits on p.PersonnelId equals pu.PersonnelId into puGroup
                                   select new PersonnelDTO()
                                   {
                                       PersonnelId = p.PersonnelId,
                                       FirstName = p.FirstName,
                                       LastName = p.LastName,
                                       MiddleName = p.MiddleName,
                                       PreferredName = p.PreferredName,
                                       UwempNo = p.UwempNo,
                                       UwnetId = p.UwnetId,
                                       Title = p.Title,
                                       PersonStartDate = p.PersonStartDate,
                                       PersonEndDate = p.PersonEndDate,
                                       RoomNumber = p.RoomNumber,
                                       PersonPhone = p.PersonPhone,
                                       PersonEmail = p.PersonEmail,
                                       WebDirectory = p.WebDirectory,
                                       EdirectoryEntry = p.EdirectoryEntry,
                                       PersonWeb = p.PersonWeb,
                                       Faculty = p.Faculty,
                                       Staff = p.Staff,
                                       Student = p.Student,
                                       PersonnelUnitDTOs = (List<PersonnelUnitDTO>)puGroup.Select(pu => new PersonnelUnitDTO()
                                       {
                                           PersonnelId = pu.PersonnelId,
                                           UnitId = pu.UnitId,
                                           PersonUnitTitle = pu.PersonUnitTitle,
                                           RoleEffectiveDate = pu.RoleEffectiveDate,
                                           RoleEndDate = pu.RoleEndDate
                                       })
                                   }).ToListAsync();

            return personnel;
        }

        // gets personnel by FirstName and LastName
        [HttpGet("namecheck/{FirstName}/{LastName}")]
        public async Task<IActionResult> GetPerson(string? FirstName, string? LastName)
        {
            var result = await _context.Personnel
                .Where(e => e.FirstName == FirstName && e.LastName == LastName)
                .Select(p => new PersonnelDTO
                {
                    PersonnelId = p.PersonnelId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    MiddleName = p.MiddleName,
                    PreferredName = p.PreferredName,
                    UwempNo = p.UwempNo,
                    UwnetId = p.UwnetId,
                    Title = p.Title,
                    PersonStartDate = p.PersonStartDate,
                    PersonEndDate = p.PersonEndDate,
                    RoomNumber = p.RoomNumber,
                    PersonPhone = p.PersonPhone,
                    PersonEmail = p.PersonEmail,
                    WebDirectory = p.WebDirectory,
                    EdirectoryEntry = p.EdirectoryEntry,
                    PersonWeb = p.PersonWeb,
                    Faculty = p.Faculty,
                    Staff = p.Staff,
                    Student = p.Student
                })
                .ToListAsync();

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        // deletes personnel by ID
        [HttpDelete("deleteperson/{PersonnelId:int}")]
        public async Task<IActionResult> Delete(int PersonnelId)
        {
            var result = await _context.Personnel.FirstOrDefaultAsync(e => e.PersonnelId == PersonnelId);
            if (result != null)
            {
                _context.Personnel.Remove(result);
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        // POST api/<PersonnelController> adds personnel
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Personnel>> PostPersonnel([FromBody] Personnel personel)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Personnel> entityEntry = _context.Personnel.Add(personel);
            await _context.SaveChangesAsync();
            return Ok(personel);
        }

        // PUT api/<PersonnelController>/5 updates personnel by ID
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<Personnel> Put([FromBody] Personnel personnel)

        {
            var result = await _context.Personnel
                .FirstOrDefaultAsync(e => e.PersonnelId == personnel.PersonnelId);
            if (result != null)
            {
                result.PersonnelId = personnel.PersonnelId;
                result.RoomNumber = personnel.RoomNumber;
                result.LastName = personnel.LastName;
                result.FirstName = personnel.FirstName;
                result.MiddleName = personnel.MiddleName;
                result.PreferredName = personnel.PreferredName;
                result.PersonWeb = personnel.PersonWeb;
                result.UwempNo = personnel.UwempNo;
                result.UwnetId = personnel.UwnetId;
                result.Title = personnel.Title;
                result.PersonStartDate = personnel.PersonStartDate;
                result.PersonEndDate = personnel.PersonEndDate;
                result.PersonPhone = personnel.PersonPhone;
                result.MaintenanceDate = personnel.MaintenanceDate;
                result.MaintenanceUser = personnel.MaintenanceUser;
                result.MaintenanceApp = personnel.MaintenanceApp;
                result.CreationUser = personnel.CreationUser;
                result.CreationApp = personnel.CreationApp;

                await _context.SaveChangesAsync();

                return result;
            }
            else
            {
                return null;
            }
        }

    }
}
