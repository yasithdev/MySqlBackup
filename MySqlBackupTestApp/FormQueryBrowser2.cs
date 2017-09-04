﻿using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormQueryBrowser2 : Form
    {
        private DataTable dt = new DataTable();

        public FormQueryBrowser2()
        {
            InitializeComponent();

            dataGridView1.VirtualMode = true;
            dataGridView1.CellValueNeeded += dataGridView1_CellValueNeeded;
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (e.RowIndex >= dt.Rows.Count)
                    return;
                if (e.ColumnIndex >= dt.Columns.Count)
                    return;

                if (dt.Rows[e.RowIndex][e.ColumnIndex] == null || dt.Rows[e.RowIndex][e.ColumnIndex] is DBNull)
                {
                    e.Value = "null";
                    return;
                }

                var dtype = dt.Columns[e.ColumnIndex].DataType;

                if (dtype == typeof(byte[]))
                    e.Value = "blob/byte[]";
                else if (dtype == typeof(DateTime))
                    e.Value = ((DateTime) dt.Rows[e.RowIndex][e.ColumnIndex]).ToString("yyyy-MM-dd HH:mm:ss");
                else
                    e.Value = dt.Rows[e.RowIndex][e.ColumnIndex] + "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error has occured.\r\n\r\n" + ex);
                Close();
            }
        }

        private void btSQL_Click(object sender, EventArgs e)
        {
            ExecuteSQL();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                textBox1.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Enter)
            {
                ExecuteSQL();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                textBox1.Clear();
                e.SuppressKeyPress = true;
            }
        }

        private void ExecuteSQL()
        {
            try
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                dt = new DataTable();

                var sql = textBox1.Text;

                var sqllower = sql.ToLower();

                var isExecution = false;

                using (var conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (var cmd = new MySqlCommand())
                    {
                        conn.Open();
                        cmd.Connection = conn;

                        if (sqllower.StartsWith("select") || sqllower.StartsWith("show"))
                        {
                            cmd.CommandText = sql;
                            var da = new MySqlDataAdapter(cmd);
                            da.Fill(dt);
                        }
                        else
                        {
                            isExecution = true;

                            cmd.CommandText = sql;
                            var rowsAffected = cmd.ExecuteNonQuery();

                            dt.Columns.Add("Result");

                            if (rowsAffected < 2)
                                dt.Rows.Add(rowsAffected + " row affected by the last command, no resultset returned.");
                            else
                                dt.Rows.Add(rowsAffected +
                                            " rows affected by the last command, no resultset returned.");
                        }

                        conn.Close();
                    }
                }

                foreach (DataColumn dc in dt.Columns)
                {
                    var dgvTB = new DataGridViewTextBoxColumn();
                    dgvTB.HeaderText = dc.ColumnName;
                    dataGridView1.Columns.Add(dgvTB);
                    if (isExecution)
                        dgvTB.Width = 700;
                    else
                        dgvTB.Width = (int) numericUpDown1.Value;
                }

                dataGridView1.RowTemplate.Height = 25;

                if (dt.Rows.Count > 0)
                    dataGridView1.Rows.Add(dt.Rows.Count);
                dataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                var err = ex.ToString();
                dt = new DataTable();
                dt.Columns.Add("Error");
                dt.Rows.Add(err);

                dataGridView1.RowTemplate.Height = 300;

                var dgvTB = new DataGridViewTextBoxColumn();
                dgvTB.Width = 750;
                dgvTB.HeaderText = "Error";
                dataGridView1.Columns.Add(dgvTB);
                dataGridView1.Rows.Add(1);

                dataGridView1.ClearSelection();
            }
        }

        private void FormQueryBrowser2_Load(object sender, EventArgs e)
        {
            ExecuteSQL();
        }
    }
}