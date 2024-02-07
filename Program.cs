using CsvHelper;
using Dapper;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Globalization;
using System.Text;

var localFilePath = ConfigurationManager.AppSettings["LocalPath"];
var filePrepend = ConfigurationManager.AppSettings["FilePrepend"];
var dayInterval = int.Parse(ConfigurationManager.AppSettings["DayInterval"] ?? "7");
var dateFormat = "yyyyMMdd";
var fullLocalFilePath = $"{localFilePath}/{filePrepend}_{DateTime.Now.ToString(dateFormat)}";

var _getUniqueUsersLast7Days = @"
    SELECT u.ChrisId
    FROM usersession us
        JOIN user u ON (u.ChrisId = us.ChrisId)
    WHERE us.StartTimeUtc >= DATE(NOW() - INTERVAL @DaysInThePast DAY)
    GROUP BY u.ChrisId;";

var _getSessionDataByUser = @"
    SELECT 
	    u.FName AS FirstName,
        u.LName AS LastName,
        u.ChrisId,
        us.StartTimeUtc AS StartTime,
        us.DifficultyLevelId AS DifficultyLevel,
        sm.Name AS SessionMode,
        uss.StepId AS StepNumber,
        s.Name AS StepName,
        uss.Attempt AS Attempts,
        uss.Duration,
        sta.Name AS Status,
        rs.ResultMessage
    FROM usersession us
	    JOIN user u ON (u.ChrisId = us.ChrisId)
        JOIN sessionmode sm ON (us.SessionModeId = sm.SessionModeId)
        JOIN usersessionstep uss ON (uss.UserSessionId = us.SessionId)
        JOIN step s ON (s.StepId = uss.StepId)
        LEFT JOIN usersessionstepresult ussr ON (ussr.UserSessionStepId = uss.UserSessionStepId)
        LEFT JOIN resultstatus rs ON (ussr.ResultStatusId = rs.ResultStatusId)
        LEFT JOIN status sta ON (rs.StatusId = sta.StatusId)
    WHERE us.StartTimeUtc >= DATE(NOW() - INTERVAL @DaysInThePast DAY) AND u.ChrisId = @ChrisId
    ORDER BY us.StartTimeUtc, uss.StepId, uss.Attempt;";

var _getAllSessionData = @"
    SELECT 
	    u.FName AS FirstName,
        u.LName AS LastName,
        u.ChrisId,
        us.StartTimeUtc AS StartTime,
        us.DifficultyLevelId AS DifficultyLevel,
        sm.Name AS SessionMode,
        uss.StepId AS StepNumber,
        s.Name AS StepName,
        uss.Attempt AS Attempts,
        uss.Duration,
        sta.Name AS Status,
        rs.ResultMessage
    FROM usersession us
	    JOIN user u ON (u.ChrisId = us.ChrisId)
        JOIN sessionmode sm ON (us.SessionModeId = sm.SessionModeId)
        JOIN usersessionstep uss ON (uss.UserSessionId = us.SessionId)
        JOIN step s ON (s.StepId = uss.StepId)
        LEFT JOIN usersessionstepresult ussr ON (ussr.UserSessionStepId = uss.UserSessionStepId)
        LEFT JOIN resultstatus rs ON (ussr.ResultStatusId = rs.ResultStatusId)
        LEFT JOIN status sta ON (rs.StatusId = sta.StatusId)
    WHERE us.StartTimeUtc >= DATE(NOW() - INTERVAL @DaysInThePast DAY)
    ORDER BY us.StartTimeUtc, uss.StepId, uss.Attempt;";

var connectionString = "server=mars-troubleshootingvr.mysql.database.azure.com;uid=marsadmin;pwd=RedZebra10202023330!;database=dbo";
using var connection = new MySqlConnection(connectionString);

FileInfo fileInfo = new FileInfo(fullLocalFilePath);
if (!fileInfo.Exists)
{
    Directory.CreateDirectory(fullLocalFilePath);
}

var usersParameters = new DynamicParameters();
usersParameters.Add("@DaysInThePast", dayInterval);
var users = connection.Query<string>(_getUniqueUsersLast7Days, usersParameters);
foreach (var item in users)
{
    var sessionDataByUserParameters = new DynamicParameters();
    sessionDataByUserParameters.Add("@ChrisId", item);
    sessionDataByUserParameters.Add("@DaysInThePast", dayInterval);
    var sessionDataByUser = connection.Query<SessionData>(_getSessionDataByUser, sessionDataByUserParameters);
    using (var writer = new StreamWriter($"{fullLocalFilePath}/{filePrepend}_{DateTime.Now.ToString(dateFormat)}_{item}.csv", false, Encoding.UTF8))
    {
        var csv = new CsvWriter(writer, CultureInfo.CurrentCulture);
        csv.WriteRecords(sessionDataByUser);
        csv.Flush();
    }
}

var allSessionDataParameters = new DynamicParameters();
allSessionDataParameters.Add("@DaysInThePast", dayInterval);
var allSessionData = connection.Query<SessionData>(_getAllSessionData, allSessionDataParameters);
using (var writer = new StreamWriter($"{fullLocalFilePath}/{filePrepend}_{DateTime.Now.ToString(dateFormat)}_ALL.csv", false, Encoding.UTF8))
{
    var csv = new CsvWriter(writer, CultureInfo.CurrentCulture);
    csv.WriteRecords(allSessionData);
    csv.Flush();
}

class SessionData
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ChrisId { get; set; }
    public string StartTime { get; set; }
    public string DifficultyLevel { get; set; }
    public string SessionMode { get; set; }
    public string StepNumber { get; set; }
    public string StepName { get; set; }
    public string Attempts { get; set; }
    public string Duration { get; set; }
    public string Status { get; set; }
    public string ResultMessage { get; set; }
}