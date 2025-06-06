using AssistantApp.Models;
using Npgsql;

namespace AssistantApp.Data
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<SymptomCategory>> GetAllCategoriesAsync()
        {
            var categories = new List<SymptomCategory>();
            const string sql = "SELECT id, name FROM symptom_categories ORDER BY name;";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new SymptomCategory
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return categories;
        }

        public async Task<List<Symptom>> GetAllSymptomsAsync()
        {
            var symptoms = new List<Symptom>();
            const string sql = "SELECT id, name, category_id FROM symptoms ORDER BY name;";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                symptoms.Add(new Symptom
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CategoryId = reader.GetInt32(2)
                });
            }
            return symptoms;
        }

        public async Task<List<Diagnosis>> GetAllDiagnosesAsync()
        {
            var diagnoses = new List<Diagnosis>();
            const string sql = "SELECT id, name, description FROM diagnoses ORDER BY name;";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                diagnoses.Add(new Diagnosis
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return diagnoses;
        }

        public async Task<Diagnosis> GetDiagnosisByIdAsync(int diagnosisId)
        {
            Diagnosis diagnosis = null;
            const string sql = "SELECT id, name, description FROM diagnoses WHERE id = @id;";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", diagnosisId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                diagnosis = new Diagnosis
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            return diagnosis;
        }

        public async Task<int> SaveUsageRecordAsync(List<int> symptomIds, int? diagnosisId)
        {
            int usageId;
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            const string insertUsageSql = @"
                INSERT INTO usage_records (event_time, diagnosis_id)
                VALUES (NOW(), @diagId)
                RETURNING id;
            ";
            await using (var cmdUsage = new NpgsqlCommand(insertUsageSql, conn))
            {
                if (diagnosisId.HasValue)
                    cmdUsage.Parameters.AddWithValue("diagId", diagnosisId.Value);
                else
                    cmdUsage.Parameters.AddWithValue("diagId", DBNull.Value);

                usageId = Convert.ToInt32(await cmdUsage.ExecuteScalarAsync());
            }
            const string insertUsageSymptomSql = @"
                INSERT INTO usage_symptoms (usage_id, symptom_id)
                VALUES (@usageId, @symptomId);
            ";
            foreach (var symId in symptomIds)
            {
                await using var cmdUS = new NpgsqlCommand(insertUsageSymptomSql, conn);
                cmdUS.Parameters.AddWithValue("usageId", usageId);
                cmdUS.Parameters.AddWithValue("symptomId", symId);
                await cmdUS.ExecuteNonQueryAsync();
            }
            return usageId;
        }

        public async Task<List<UsageRecord>> GetUsageRecordsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var records = new List<UsageRecord>();
            string baseSql = "SELECT id, event_time, diagnosis_id FROM usage_records";
            string whereClause = "";
            var parameters = new List<NpgsqlParameter>();
            if (fromDate.HasValue && toDate.HasValue)
            {
                whereClause = " WHERE event_time BETWEEN @from AND @to";
                parameters.Add(new NpgsqlParameter("from", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = fromDate.Value });
                parameters.Add(new NpgsqlParameter("to", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = toDate.Value });
            }
            else if (fromDate.HasValue)
            {
                whereClause = " WHERE event_time >= @from";
                parameters.Add(new NpgsqlParameter("from", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = fromDate.Value });
            }
            else if (toDate.HasValue)
            {
                whereClause = " WHERE event_time <= @to";
                parameters.Add(new NpgsqlParameter("to", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = toDate.Value });
            }
            string finalSql = baseSql + whereClause + " ORDER BY event_time DESC;";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(finalSql, conn);
            foreach (var param in parameters)
                cmd.Parameters.Add(param);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new UsageRecord
                {
                    Id = reader.GetInt32(0),
                    EventTime = reader.GetDateTime(1),
                    DiagnosisId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2)
                });
            }
            return records;
        }

        public async Task<List<Symptom>> GetSymptomsByUsageIdAsync(int usageId)
        {
            var result = new List<Symptom>();
            const string sql = @"
                SELECT s.id, s.name, s.category_id 
                FROM symptoms AS s
                INNER JOIN usage_symptoms AS us ON s.id = us.symptom_id
                WHERE us.usage_id = @usageId
                ORDER BY s.name;
            ";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("usageId", usageId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Symptom
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CategoryId = reader.GetInt32(2)
                });
            }
            return result;
        }
    }
}
