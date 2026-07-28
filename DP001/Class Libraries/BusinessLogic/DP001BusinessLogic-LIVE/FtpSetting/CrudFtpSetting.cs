using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudFtpSetting
    {
        public List<FTPSetting> Read(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.FTPSettings
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.Suppliers)
                    .Where(x => x.ChannelFK == channelId).ToList();
            }
        }

        public FTPSetting ReadByKey(int ftpSettingId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.FTPSettings
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.FieldMapping)
                    .Where(x => x.FTPSettingsID == ftpSettingId)
                    .FirstOrDefault();
            }
        }

        public List<FTPSetting> Read(Expression<Func<FTPSetting, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.FTPSettings
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.Suppliers)
                    .Include(x => x.FieldMapping)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public FTPSetting Create(FTPSetting obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var ftpSetting = db.FTPSettings.Find(obj.FTPSettingsID);

                if (ftpSetting == null)
                {
                    ftpSetting = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                if (obj.Suppliers != null)
                {
                    foreach (var sup in obj.Suppliers)
                    {
                        var crudSup = new CrudSupplier();
                        sup.FTPSettingsFK = obj.FTPSettingsID;
                        sup.ChannelFK = obj.ChannelFK;
                        crudSup.Create(sup);
                    }
                }

                return ftpSetting;
            }
        }

        public void Update(FTPSetting obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.FieldMapping != null)
                {
                    db.Entry(obj.FieldMapping).State = EntityState.Modified;
                }
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();

                if (obj.Suppliers != null)
                {
                    foreach (var sup in obj.Suppliers)
                    {
                        var crudSup = new CrudSupplier();
                        sup.FTPSettingsFK = obj.FTPSettingsID;
                        sup.ChannelFK = obj.ChannelFK;
                        crudSup.Update(sup);
                    }
                }
            }
        }

        public bool Delete(FTPSetting obj)
        {
            bool isSuccess = false;

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@FtpSettingID", SqlDbType.Int);
            sqlParm1.Value = obj.FTPSettingsID;
            sqlParms.Add(sqlParm1);
            SqlParameter sqlParm2 = new SqlParameter("@FileType", SqlDbType.VarChar);
            sqlParm2.Value = obj.Lookup.LookupName;
            sqlParms.Add(sqlParm2);

            isSuccess = SQL.ExecuteStoredProcedure("DP001", "DeleteFtpSetting", sqlParms, obj.ChannelFK);

            return isSuccess;
        }
    }
}
