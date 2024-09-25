using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Dapper;
using PersonnelApi.Models;

namespace PersonnelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonnelUnitController : ControllerBase
    {
        private readonly LawSchoolContext _context;
        private readonly IConfiguration _configuration;

        public PersonnelUnitController(LawSchoolContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST api/<PersonnelUnitController>
        [HttpPost]
        public async Task<ActionResult<PersonnelUnit>> PostPersonnelUnit([FromBody] PersonnelUnit personelUnit)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<PersonnelUnit> entityEntry = _context.PersonnelUnits.Add(personelUnit);
            await _context.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        // GET: api/<PersonnelUnitController>
        [HttpGet("{PersonnelId:int} / {UnitId:int} / {RoleId:alpha}", Name = "GetPersonnelUnit")]
        public async Task<PersonnelUnit> GetPersonnelUnit(int PersonnelId, int UnitId, string RoleId)
        {
            return await _context.PersonnelUnits.FirstOrDefaultAsync(e => e.PersonnelId == PersonnelId && e.UnitId == UnitId && e.RoleId == RoleId);
        }

        // PUT api/<PersonnelUnitController>/5 updates title
        [HttpPut("updatetitle")]
        public async Task<PersonnelUnit> Put([FromBody] PersonnelUnit personnelUnit)

        {
            var result = await _context.PersonnelUnits
                .FirstOrDefaultAsync(e => e.PersonnelId == personnelUnit.PersonnelId && e.UnitId == personnelUnit.UnitId && e.RoleId == personnelUnit.RoleId);
            if (result != null)
            {
                result.RoomNumber = personnelUnit.RoomNumber;
                result.RoleEffectiveDate = personnelUnit.RoleEffectiveDate;
                result.PersonUnitTitle = personnelUnit.PersonUnitTitle;
                result.RoleEndDate = personnelUnit.RoleEndDate;
                result.Tenure = personnelUnit.Tenure;
                result.Acting = personnelUnit.Acting;
                result.CreationDate = personnelUnit.CreationDate;
                result.PersonUnitTitle = personnelUnit.PersonUnitTitle;
                result.CreationUser = personnelUnit.CreationUser;
                result.CreationApp = personnelUnit.CreationApp;
                result.MaintenanceDate = personnelUnit.MaintenanceDate;
                result.MaintenanceUser = personnelUnit.MaintenanceUser;
                result.MaintenanceApp = personnelUnit.MaintenanceApp;

                await _context.SaveChangesAsync();

                return result;
            }
            var resultnext = await _context.PersonnelUnits
                .FirstOrDefaultAsync(e => e.PersonnelId == personnelUnit.PersonnelId && e.UnitId == personnelUnit.UnitId);
            if (resultnext != null)
            {
                resultnext.RoomNumber = personnelUnit.RoomNumber;
                resultnext.RoleEffectiveDate = personnelUnit.RoleEffectiveDate;
                resultnext.PersonUnitTitle = personnelUnit.PersonUnitTitle;
                resultnext.RoleId = personnelUnit.RoleId;
                resultnext.RoleEndDate = personnelUnit.RoleEndDate;
                resultnext.Tenure = personnelUnit.Tenure;
                resultnext.Acting = personnelUnit.Acting;
                resultnext.CreationDate = personnelUnit.CreationDate;
                resultnext.PersonUnitTitle = personnelUnit.PersonUnitTitle;
                resultnext.CreationUser = personnelUnit.CreationUser;
                resultnext.CreationApp = personnelUnit.CreationApp;
                resultnext.MaintenanceDate = personnelUnit.MaintenanceDate;
                resultnext.MaintenanceUser = personnelUnit.MaintenanceUser;
                resultnext.MaintenanceApp = personnelUnit.MaintenanceApp;

                await _context.SaveChangesAsync();

                return resultnext;
            }
            else
            {
                return null;
            }
        }

        // PUT api/<PersonnelUnitController>/5 removes title
        [HttpPut("removetitle")]
        public async Task<PersonnelUnit> RemoveTitle([FromBody] PersonnelUnit personnelUnit)

        {
            var connectionString = _configuration.GetConnectionString("DbConnection");
            var result = await _context.PersonnelUnits
                .FirstOrDefaultAsync(e => e.PersonnelId == personnelUnit.PersonnelId && e.UnitId == personnelUnit.UnitId && e.RoleId == personnelUnit.RoleId);
            if (result != null)
                using (var connection = new SqlConnection(connectionString))
                {
                    var sqlStatement = "DELETE PersonnelUnit WHERE PersonnelId = @PersonnelId AND UnitId = @UnitId AND RoleId = @RoleId";
                    var sqlStatementNew = "DELETE PersonnelUnit WHERE PersonnelId = @PersonnelId AND UnitId = @UnitId";
                    object temp;
                    if (personnelUnit.RoleId == null)
                    {
                        var affectedRowsNew = connection.Execute(sqlStatementNew, new { PersonnelId = personnelUnit.PersonnelId, UnitId = personnelUnit.UnitId });
                        return result;
                    }
                    else
                    {
                        temp = personnelUnit.RoleId;
                    }
                    var affectedRows = connection.Execute(sqlStatement, new { PersonnelId = personnelUnit.PersonnelId, UnitId = personnelUnit.UnitId, RoleId = temp });
                    return result;
                }
            else
            {
                return null;
            }
        }

        // DELETE api/<PersonnelUnitController>/5 removes personnel unit by personnel id, unit id, and role id
        [HttpDelete("deleteroletitle/{PersonnelId:int} / {UnitId:int} / {RoleId:alpha}")]
        public async Task<IActionResult> DeleteRoleTitle(int PersonnelId, int UnitId, string RoleId)
        {
            var connectionString = _configuration.GetConnectionString("DbConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlStatement = "DELETE PersonnelUnit WHERE PersonnelId = @PersonnelId AND UnitId = @UnitId AND RoleId = @RoleId";
                var affectedRows = connection.Execute(sqlStatement, new { PersonnelId = PersonnelId, UnitId = UnitId, RoleId = RoleId });
            }
            return Ok();
        }

    }
}
