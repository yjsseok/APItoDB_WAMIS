using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using log4net;
using APItoDB_WAMIS.A_Models;

namespace APItoDB_WAMIS.A_Services
{
    public class asos_DataService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(asos_DataService));
        private readonly string connectionString;

        private readonly Action<string> logAction;

        public asos_DataService(string connectionString, Action<string> logAction)
        {
            this.connectionString = connectionString;
            this.logAction = logAction;
        }

        public async Task EnsureTablesExistAsync()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = 'drought' 
                        AND table_name = 'tb_kma_asos_dtdata'";
                    
                    using (var cmd = new NpgsqlCommand(checkTableQuery, connection))
                    {
                        var tableExists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                        
                        if (!tableExists)
                        {
                            log.Info("drought.tb_kma_asos_dtdata 테이블이 존재하지 않습니다. 테이블 생성을 건너뜁니다.");
                        }
                        else
                        {
                            log.Info("drought.tb_kma_asos_dtdata 테이블이 확인되었습니다.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("테이블 존재 확인 중 오류 발생", ex);
                throw;
            }
        }

        public async Task<int> InsertWeatherDataBulkAsync(List<ASOS_WeatherData> weatherDataList)
        {
            if (weatherDataList == null || !weatherDataList.Any())
                return 0;

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var insertQuery = @"
                        INSERT INTO drought.tb_kma_asos_dtdata (
                            tm, wd, gst_wd, gst_tm, ps, pr, td, pv, rn_day, sd_hr3, sd_tot, wp, ca_tot, 
                            ch_min, ct_top, ct_low, si, ts, te_01, te_03, wh, ir, rn_jun, wr_day, ws_max, 
                            wd_ins, ws_ins_tm, ta_max, ta_min, td_avg, tg_min, hm_min, pv_avg, ev_l, pa_avg, 
                            ps_max, ps_min, ss_day, ss_cmb, rn_d99, sd_new, sd_max, te_05, te_15, te_50, 
                            tmst, dtm, dir, lat, val, stn, ws, gst_ws, pa, pt, ta, hm, rn, rn_int, sd_day, 
                            wc, ww, ca_mid, ct, ct_mid, ss, st_gd, te_005, te_02, st_sea, bf, ix, ws_avg, 
                            wd_max, ws_max_tm, ws_ins, ta_avg, ta_max_tm, ta_min_tm, ts_avg, hm_avg, hm_min_tm, 
                            ev_s, fg_dur, ps_avg, ps_max_tm, ps_min_tm, ss_dur, si_day, rn_dur, sd_new_tm, 
                            sd_max_tm, te_10, te_30, s, tmed, st, lon, ht
                        ) VALUES (
                            @tm, @wd, @gst_wd, @gst_tm, @ps, @pr, @td, @pv, @rn_day, @sd_hr3, @sd_tot, @wp, @ca_tot, 
                            @ch_min, @ct_top, @ct_low, @si, @ts, @te_01, @te_03, @wh, @ir, @rn_jun, @wr_day, @ws_max, 
                            @wd_ins, @ws_ins_tm, @ta_max, @ta_min, @td_avg, @tg_min, @hm_min, @pv_avg, @ev_l, @pa_avg, 
                            @ps_max, @ps_min, @ss_day, @ss_cmb, @rn_d99, @sd_new, @sd_max, @te_05, @te_15, @te_50, 
                            @tmst, @dtm, @dir, @lat, @val, @stn, @ws, @gst_ws, @pa, @pt, @ta, @hm, @rn, @rn_int, @sd_day, 
                            @wc, @ww, @ca_mid, @ct, @ct_mid, @ss, @st_gd, @te_005, @te_02, @st_sea, @bf, @ix, @ws_avg, 
                            @wd_max, @ws_max_tm, @ws_ins, @ta_avg, @ta_max_tm, @ta_min_tm, @ts_avg, @hm_avg, @hm_min_tm, 
                            @ev_s, @fg_dur, @ps_avg, @ps_max_tm, @ps_min_tm, @ss_dur, @si_day, @rn_dur, @sd_new_tm, 
                            @sd_max_tm, @te_10, @te_30, @s, @tmed, @st, @lon, @ht
                        )
                        ON CONFLICT (stn, tm) DO UPDATE SET
                            wd = EXCLUDED.wd, gst_wd = EXCLUDED.gst_wd, gst_tm = EXCLUDED.gst_tm, ps = EXCLUDED.ps, 
                            pr = EXCLUDED.pr, td = EXCLUDED.td, pv = EXCLUDED.pv, rn_day = EXCLUDED.rn_day, 
                            sd_hr3 = EXCLUDED.sd_hr3, sd_tot = EXCLUDED.sd_tot, wp = EXCLUDED.wp, ca_tot = EXCLUDED.ca_tot, 
                            ch_min = EXCLUDED.ch_min, ct_top = EXCLUDED.ct_top, ct_low = EXCLUDED.ct_low, si = EXCLUDED.si, 
                            ts = EXCLUDED.ts, te_01 = EXCLUDED.te_01, te_03 = EXCLUDED.te_03, wh = EXCLUDED.wh, 
                            ir = EXCLUDED.ir, rn_jun = EXCLUDED.rn_jun, wr_day = EXCLUDED.wr_day, ws_max = EXCLUDED.ws_max, 
                            wd_ins = EXCLUDED.wd_ins, ws_ins_tm = EXCLUDED.ws_ins_tm, ta_max = EXCLUDED.ta_max, 
                            ta_min = EXCLUDED.ta_min, td_avg = EXCLUDED.td_avg, tg_min = EXCLUDED.tg_min, 
                            hm_min = EXCLUDED.hm_min, pv_avg = EXCLUDED.pv_avg, ev_l = EXCLUDED.ev_l, 
                            pa_avg = EXCLUDED.pa_avg, ps_max = EXCLUDED.ps_max, ps_min = EXCLUDED.ps_min, 
                            ss_day = EXCLUDED.ss_day, ss_cmb = EXCLUDED.ss_cmb, rn_d99 = EXCLUDED.rn_d99, 
                            sd_new = EXCLUDED.sd_new, sd_max = EXCLUDED.sd_max, te_05 = EXCLUDED.te_05, 
                            te_15 = EXCLUDED.te_15, te_50 = EXCLUDED.te_50, tmst = EXCLUDED.tmst, dtm = EXCLUDED.dtm, 
                            dir = EXCLUDED.dir, lat = EXCLUDED.lat, val = EXCLUDED.val, ws = EXCLUDED.ws, 
                            gst_ws = EXCLUDED.gst_ws, pa = EXCLUDED.pa, pt = EXCLUDED.pt, ta = EXCLUDED.ta, 
                            hm = EXCLUDED.hm, rn = EXCLUDED.rn, rn_int = EXCLUDED.rn_int, sd_day = EXCLUDED.sd_day, 
                            wc = EXCLUDED.wc, ww = EXCLUDED.ww, ca_mid = EXCLUDED.ca_mid, ct = EXCLUDED.ct, 
                            ct_mid = EXCLUDED.ct_mid, ss = EXCLUDED.ss, st_gd = EXCLUDED.st_gd, te_005 = EXCLUDED.te_005, 
                            te_02 = EXCLUDED.te_02, st_sea = EXCLUDED.st_sea, bf = EXCLUDED.bf, ix = EXCLUDED.ix, 
                            ws_avg = EXCLUDED.ws_avg, wd_max = EXCLUDED.wd_max, ws_max_tm = EXCLUDED.ws_max_tm, 
                            ws_ins = EXCLUDED.ws_ins, ta_avg = EXCLUDED.ta_avg, ta_max_tm = EXCLUDED.ta_max_tm, 
                            ta_min_tm = EXCLUDED.ta_min_tm, ts_avg = EXCLUDED.ts_avg, hm_avg = EXCLUDED.hm_avg, 
                            hm_min_tm = EXCLUDED.hm_min_tm, ev_s = EXCLUDED.ev_s, fg_dur = EXCLUDED.fg_dur, 
                            ps_avg = EXCLUDED.ps_avg, ps_max_tm = EXCLUDED.ps_max_tm, ps_min_tm = EXCLUDED.ps_min_tm, 
                            ss_dur = EXCLUDED.ss_dur, si_day = EXCLUDED.si_day, rn_dur = EXCLUDED.rn_dur, 
                            sd_new_tm = EXCLUDED.sd_new_tm, sd_max_tm = EXCLUDED.sd_max_tm, te_10 = EXCLUDED.te_10, 
                            te_30 = EXCLUDED.te_30, s = EXCLUDED.s, tmed = EXCLUDED.tmed, st = EXCLUDED.st, 
                            lon = EXCLUDED.lon, ht = EXCLUDED.ht";

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int totalInserted = 0;

                            foreach (var data in weatherDataList)
                            {
                                using (var cmd = new NpgsqlCommand(insertQuery, connection, transaction))
                                {
                                    AddParameters(cmd, data);
                                    await cmd.ExecuteNonQueryAsync();
                                    totalInserted++;
                                }
                            }

                            await transaction.CommitAsync();
                            log.Info($"ASOS 기상 데이터 {totalInserted}건 저장 완료");
                            return totalInserted;
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("ASOS 기상 데이터 저장 중 오류 발생", ex);
                throw;
            }
        }

        private void AddParameters(NpgsqlCommand cmd, ASOS_WeatherData data)
        {
            cmd.Parameters.AddWithValue("@tm", data.TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wd", data.WD ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@gst_wd", data.GST_WD ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@gst_tm", data.GST_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps", data.PS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pr", data.PR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@td", data.TD ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pv", data.PV ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn_day", data.RN_DAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_hr3", data.SD_HR3 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_tot", data.SD_TOT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wp", data.WP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ca_tot", data.CA_TOT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ch_min", data.CH_MIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ct_top", data.CT_TOP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ct_low", data.CT_LOW ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@si", data.SI ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ts", data.TS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_01", data.TE_01 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_03", data.TE_03 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wh", data.WH ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ir", data.IR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn_jun", data.RN_JUN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wr_day", data.WR_DAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws_max", data.WS_MAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wd_ins", data.WD_INS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws_ins_tm", data.WS_INS_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta_max", data.TA_MAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta_min", data.TA_MIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@td_avg", data.TD_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tg_min", data.TG_MIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hm_min", data.HM_MIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pv_avg", data.PV_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ev_l", data.EV_L ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pa_avg", data.PA_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps_max", data.PS_MAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps_min", data.PS_MIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ss_day", data.SS_DAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ss_cmb", data.SS_CMB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn_d99", data.RN_D99 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_new", data.SD_NEW ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_max", data.SD_MAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_05", data.TE_05 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_15", data.TE_15 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_50", data.TE_50 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tmst", data.TMST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@dtm", data.DTM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@dir", data.DIR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lat", data.LAT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@val", data.VAL ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@stn", data.STN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws", data.WS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@gst_ws", data.GST_WS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pa", data.PA ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pt", data.PT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta", data.TA ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hm", data.HM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn", data.RN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn_int", data.RN_INT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_day", data.SD_DAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wc", data.WC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ww", data.WW ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ca_mid", data.CA_MID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ct", data.CT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ct_mid", data.CT_MID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ss", data.SS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@st_gd", data.ST_GD ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_005", data.TE_005 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_02", data.TE_02 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@st_sea", data.ST_SEA ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@bf", data.BF ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ix", data.IX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws_avg", data.WS_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wd_max", data.WD_MAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws_max_tm", data.WS_MAX_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ws_ins", data.WS_INS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta_avg", data.TA_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta_max_tm", data.TA_MAX_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ta_min_tm", data.TA_MIN_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ts_avg", data.TS_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hm_avg", data.HM_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hm_min_tm", data.HM_MIN_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ev_s", data.EV_S ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fg_dur", data.FG_DUR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps_avg", data.PS_AVG ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps_max_tm", data.PS_MAX_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ps_min_tm", data.PS_MIN_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ss_dur", data.SS_DUR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@si_day", data.SI_DAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rn_dur", data.RN_DUR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_new_tm", data.SD_NEW_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sd_max_tm", data.SD_MAX_TM ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_10", data.TE_10 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@te_30", data.TE_30 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@s", data.S ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tmed", data.TMED ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@st", data.ST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lon", data.LON ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ht", data.HT ?? (object)DBNull.Value);
        }

        public async Task<List<DateTime>> GetMissingDatesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        WITH date_series AS (
                            SELECT generate_series(@startDate::date, @endDate::date, '1 day'::interval)::date as date_val
                        )
                        SELECT ds.date_val
                        FROM date_series ds
                        LEFT JOIN drought.tb_kma_asos_dtdata asos 
                            ON ds.date_val::text = LEFT(asos.tm, 8)
                        WHERE asos.tm IS NULL
                        ORDER BY ds.date_val";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);

                        var missingDates = new List<DateTime>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                missingDates.Add(reader.GetDateTime(0));
                            }
                        }

                        return missingDates;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("누락된 날짜 조회 중 오류 발생", ex);
                throw;
            }
        }
    }
}