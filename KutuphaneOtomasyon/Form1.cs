using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace KutuphaneOtomasyon
{
    // --- 1. VERİ MODELLERİ ---
    public class KutuphaneVerisi
    {
        public List<Kitap> Kitaplar { get; set; } = new List<Kitap>();
        public List<Uye> Uyeler { get; set; } = new List<Uye>();
        public List<IslemKaydi> Gecmis { get; set; } = new List<IslemKaydi>();
        public bool IsKaranlikMod { get; set; } = false;
    }

    public class IslemKaydi
    {
        public DateTime Tarih { get; set; }
        public string Kullanici { get; set; } = string.Empty;
        public string Islem { get; set; } = string.Empty;
        public string KitapAdi { get; set; } = string.Empty;
    }

    public class Kitap
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Yazar { get; set; } = string.Empty;
        public string Kategori { get; set; } = string.Empty;
        public bool RaftaMi { get; set; }
        public int? OduncAlanUyeId { get; set; }
        public DateTime? OduncAlinmaTarihi { get; set; }
        public Kitap() { }
        public Kitap(int id, string ad, string yazar, string kategori)
        {
            Id = id; Ad = ad; Yazar = yazar; Kategori = kategori; RaftaMi = true;
        }
        public string DurumBilgisiGetir(Uye aktifUye, List<Uye> tumUyeler)
        {
            if (RaftaMi) return "Mevcut";
            if (OduncAlanUyeId == aktifUye.Id) return "Sizde";
            if (aktifUye.IsAdmin && OduncAlanUyeId.HasValue)
            {
                var alanKisi = tumUyeler.FirstOrDefault(u => u.Id == OduncAlanUyeId.Value);
                return alanKisi != null ? $"Kimde: {alanKisi.KullaniciAdi}" : "Ödünçte";
            }
            return "Ödünçte";
        }
    }

    public class Uye
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public Uye() { }
        public Uye(int id, string kAdi, string sifre, bool isAdmin = false)
        {
            Id = id; KullaniciAdi = kAdi; Sifre = sifre; IsAdmin = isAdmin;
        }
    }

    // --- 2. SİSTEM MANTIĞI ---
    public class KutuphaneSistemi
    {
        public List<Kitap> Kitaplar { get; private set; } = new List<Kitap>();
        public List<Uye> Uyeler { get; private set; } = new List<Uye>();
        public List<IslemKaydi> Gecmis { get; private set; } = new List<IslemKaydi>();
        public bool IsKaranlikMod { get; set; }
        public Uye AktifUye { get; set; } = null!;
        private readonly string _dosyaYolu = "kutuphane_verisi.json";

        public KutuphaneSistemi() { VerileriYukle(); }
        private void VerileriYukle()
        {
            if (File.Exists(_dosyaYolu))
            {
                string json = File.ReadAllText(_dosyaYolu);
                var veri = JsonSerializer.Deserialize<KutuphaneVerisi>(json);
                if (veri != null) { Kitaplar = veri.Kitaplar; Uyeler = veri.Uyeler; Gecmis = veri.Gecmis; IsKaranlikMod = veri.IsKaranlikMod; }
            }
            if (!Uyeler.Any(u => u.IsAdmin)) Uyeler.Add(new Uye(0, "admin", "admin", true));
            if (Kitaplar.Count == 0) VarsayilanKitaplariYukle();
            VerileriKaydet();
        }
        private void VarsayilanKitaplariYukle()
        {
            string[] kats = { "Roman", "Tarih", "Bilim", "Yazılım", "Felsefe" };
            int id = 1;
            foreach (var kat in kats) for (int i = 1; i <= 5; i++) Kitaplar.Add(new Kitap(id++, $"{kat} Kitabı {i}", "Yazar Bilgisi", kat));
        }
        public void VerileriKaydet()
        {
            var veri = new KutuphaneVerisi { Kitaplar = this.Kitaplar, Uyeler = this.Uyeler, Gecmis = this.Gecmis, IsKaranlikMod = this.IsKaranlikMod };
            File.WriteAllText(_dosyaYolu, JsonSerializer.Serialize(veri, new JsonSerializerOptions { WriteIndented = true }));
        }
        public bool KayitOl(string kAdi, string sifre)
        {
            if (string.IsNullOrWhiteSpace(kAdi) || Uyeler.Any(u => u.KullaniciAdi == kAdi)) return false;
            Uyeler.Add(new Uye(Uyeler.Count > 0 ? Uyeler.Max(u => u.Id) + 1 : 1, kAdi, sifre));
            VerileriKaydet(); return true;
        }
        public bool GirisYap(string kAdi, string sifre)
        {
            var uye = Uyeler.FirstOrDefault(u => u.KullaniciAdi == kAdi && u.Sifre == sifre);
            if (uye != null) { AktifUye = uye; return true; }
            return false;
        }
        public bool KitapOduncAl(Kitap kitap)
        {
            if (kitap.RaftaMi && AktifUye != null)
            {
                kitap.RaftaMi = false; kitap.OduncAlanUyeId = AktifUye.Id;
                kitap.OduncAlinmaTarihi = DateTime.Now;
                Gecmis.Add(new IslemKaydi { Tarih = DateTime.Now, Kullanici = AktifUye.KullaniciAdi, Islem = "Ödünç Alındı", KitapAdi = kitap.Ad });
                VerileriKaydet(); return true;
            }
            return false;
        }
        public bool KitapIadeEt(Kitap kitap)
        {
            if (!kitap.RaftaMi && kitap.OduncAlanUyeId == AktifUye.Id)
            {
                kitap.RaftaMi = true; kitap.OduncAlanUyeId = null;
                kitap.OduncAlinmaTarihi = null;
                Gecmis.Add(new IslemKaydi { Tarih = DateTime.Now, Kullanici = AktifUye.KullaniciAdi, Islem = "İade Edildi", KitapAdi = kitap.Ad });
                VerileriKaydet(); return true;
            }
            return false;
        }
    }

    // --- 3. ARAYÜZ (FORM) MANTIĞI ---
    public partial class Form1 : Form
    {
        private KutuphaneSistemi _sistem = new KutuphaneSistemi();
        private Panel pnlBaslangic = null!, pnlKayit = null!, pnlGiris = null!, pnlAnaEkran = null!, pnlAdminPaneli = null!, pnlKullanicilar = null!, pnlProfil = null!, pnlLog = null!, pnlGecikenler = null!;
        private TextBox txtKayitKAdi = null!, txtKayitSifre = null!, txtGirisKAdi = null!, txtGirisSifre = null!, txtArama = null!, txtYeniKitapAdi = null!, txtYeniYazar = null!, txtEskiSifre = null!, txtYeniSifre = null!;
        private ComboBox cmbKategoriler = null!, cmbYeniKategori = null!;
        private DataGridView dgvKitaplar = null!, dgvKullanicilar = null!, dgvProfilKitaplar = null!, dgvLoglar = null!, dgvGecikenler = null!;
        private Label lblHosgeldin = null!, lblSaat = null!, lblProfilBilgi = null!;
        private System.Windows.Forms.Timer tmrSaat = null!;
        private Button btnTemaSec = null!;

        public Form1() { ArayuzuHazirla(); TemayiUygula(); EkraniGoster(pnlBaslangic); }

        private void ArayuzuHazirla()
        {
            this.Text = "Kütüphane Otomasyon Sistemi";
            this.Size = new Size(750, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            pnlBaslangic = PanelOlustur(); pnlKayit = PanelOlustur(); pnlGiris = PanelOlustur();
            pnlAnaEkran = PanelOlustur(); pnlKullanicilar = PanelOlustur(); pnlProfil = PanelOlustur();
            pnlLog = PanelOlustur(); pnlGecikenler = PanelOlustur();

            btnTemaSec = new Button { Location = new Point(680, 10), Size = new Size(40, 30), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            btnTemaSec.Click += (s, e) => { _sistem.IsKaranlikMod = !_sistem.IsKaranlikMod; _sistem.VerileriKaydet(); TemayiUygula(); };
            this.Controls.Add(btnTemaSec);

            // --- Giriş/Kayıt ---
            Label lblBas = new Label { Text = "Kütüphane Sistemi", Location = new Point(250, 150), Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true };
            Button btnGK = ButonOlustur("Kayıt Ol", 270, 250, Color.LightGreen); btnGK.Click += (s, e) => EkraniGoster(pnlKayit);
            Button btnGG = ButonOlustur("Oturum Aç", 270, 310, Color.LightBlue); btnGG.Click += (s, e) => EkraniGoster(pnlGiris);
            pnlBaslangic.Controls.AddRange(new Control[] { lblBas, btnGK, btnGG });

            pnlKayit.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => EkraniGoster(pnlBaslangic)));
            txtKayitKAdi = TextboxOlustur("Kullanıcı Adı", 270, 200); txtKayitSifre = TextboxOlustur("Şifre", 270, 250, true);
            Button btnKOk = ButonOlustur("Kaydı Tamamla", 270, 310, Color.LightGreen);
            btnKOk.Click += (s, e) => { if (_sistem.KayitOl(txtKayitKAdi.Text, txtKayitSifre.Text)) EkraniGoster(pnlBaslangic); };
            pnlKayit.Controls.AddRange(new Control[] { new Label { Text = "Üye Kaydı", Location = new Point(320, 150), Font = new Font("Arial", 12) }, txtKayitKAdi, txtKayitSifre, btnKOk });

            pnlGiris.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => EkraniGoster(pnlBaslangic)));
            txtGirisKAdi = TextboxOlustur("Kullanıcı Adı", 270, 200); txtGirisSifre = TextboxOlustur("Şifre", 270, 250, true);
            Button btnGOk = ButonOlustur("Giriş Yap", 270, 310, Color.LightBlue);
            btnGOk.Click += (s, e) => {
                if (_sistem.GirisYap(txtGirisKAdi.Text, txtGirisSifre.Text))
                {
                    pnlAdminPaneli.Visible = _sistem.AktifUye.IsAdmin;
                    lblHosgeldin.Text = "Kullanıcı: " + _sistem.AktifUye.KullaniciAdi;
                    ListeyiGuncelle(); EkraniGoster(pnlAnaEkran);
                }
                else MessageBox.Show("Hatalı Giriş!");
            };
            pnlGiris.Controls.AddRange(new Control[] { new Label { Text = "Oturum Aç", Location = new Point(320, 150), Font = new Font("Arial", 12) }, txtGirisKAdi, txtGirisSifre, btnGOk });

            // --- ANA EKRAN ---
            pnlAnaEkran.Controls.Add(SolUstGeriButonuOlustur("Çıkış", (s, e) => { txtGirisSifre.Clear(); EkraniGoster(pnlBaslangic); }));
            lblHosgeldin = new Label { Location = new Point(100, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            Button btnPrf = new Button { Text = "👤 Profilim", Name = "btnPrf", Location = new Point(280, 10), Size = new Size(110, 35), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnPrf.Tag = Color.FromArgb(220, 220, 220);
            btnPrf.Click += (s, e) => { ProfilKitaplariniGuncelle(); EkraniGoster(pnlProfil); };
            pnlAnaEkran.Controls.Add(btnPrf);

            lblSaat = new Label { Location = new Point(450, 15), AutoSize = true, Font = new Font("Consolas", 10, FontStyle.Bold) };
            tmrSaat = new System.Windows.Forms.Timer { Interval = 1000 }; tmrSaat.Tick += (s, e) => lblSaat.Text = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"); tmrSaat.Start();

            txtArama = new TextBox { Location = new Point(20, 50), Width = 300, PlaceholderText = "Ara..." };
            txtArama.TextChanged += (s, e) => ListeyiGuncelle();
            cmbKategoriler = new ComboBox { Location = new Point(330, 50), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbKategoriler.Items.AddRange(new string[] { "Tümü", "Roman", "Tarih", "Bilim", "Yazılım", "Felsefe" }); cmbKategoriler.SelectedIndex = 0;
            cmbKategoriler.SelectedIndexChanged += (s, e) => ListeyiGuncelle();

            dgvKitaplar = GridOlustur(20, 90, 690, 350);
            dgvKitaplar.DataBindingComplete += (s, e) => {
                bool dark = _sistem.IsKaranlikMod;
                foreach (DataGridViewRow row in dgvKitaplar.Rows)
                {
                    string durum = row.Cells["Durum"].Value?.ToString();
                    if (durum == "Mevcut") { row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(40, 80, 40) : Color.LightGreen; row.DefaultCellStyle.ForeColor = dark ? Color.White : Color.Black; }
                    else if (durum == "Sizde") { row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(100, 80, 20) : Color.Khaki; row.DefaultCellStyle.ForeColor = dark ? Color.White : Color.Black; }
                    else { row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(80, 40, 40) : Color.LightCoral; row.DefaultCellStyle.ForeColor = dark ? Color.White : Color.Black; }
                }
            };

            Button btnO = ButonOlustur("ÖDÜNÇ AL", 20, 460, Color.SteelBlue); btnO.Click += (s, e) => {
                var k = SecilenKitabGetir(dgvKitaplar);
                if (k != null && k.RaftaMi && _sistem.KitapOduncAl(k)) { MessageBox.Show($"'{k.Ad}' üzerinize tanımlandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); ListeyiGuncelle(); }
                else if (k != null && !k.RaftaMi) MessageBox.Show("Bu kitap başka üyede!", "Reddedildi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            Button btnI = ButonOlustur("İADE ET", 240, 460, Color.IndianRed); btnI.Click += (s, e) => {
                var k = SecilenKitabGetir(dgvKitaplar);
                if (k != null && k.OduncAlanUyeId == _sistem.AktifUye.Id && _sistem.KitapIadeEt(k)) { MessageBox.Show("Kitap iade edildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); ListeyiGuncelle(); }
                else if (k != null && k.OduncAlanUyeId != _sistem.AktifUye.Id) MessageBox.Show("Bu kitap size ait değil!", "Reddedildi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            // --- ADMIN PANELİ (SİLME BUTONU VE DÜZENLEME EKLENDİ) ---
            pnlAdminPaneli = new Panel { Name = "pnlAdm", Location = new Point(20, 520), Size = new Size(690, 150), Visible = false };
            txtYeniKitapAdi = TextboxOlustur("Kitap Adı", 10, 30); txtYeniYazar = TextboxOlustur("Yazar", 220, 30);
            cmbYeniKategori = new ComboBox { Location = new Point(430, 30), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbYeniKategori.Items.AddRange(new string[] { "Roman", "Tarih", "Bilim", "Yazılım", "Felsefe" }); cmbYeniKategori.SelectedIndex = 0;

            Button btnAdd = ButonOlustur("Ekle", 560, 28, Color.DarkSeaGreen); btnAdd.Width = 120;
            btnAdd.Click += (s, e) => {
                int yeniId = _sistem.Kitaplar.Count > 0 ? _sistem.Kitaplar.Max(k => k.Id) + 1 : 1; // Kitap kalmadığında çökmeyi önler
                _sistem.Kitaplar.Add(new Kitap(yeniId, txtYeniKitapAdi.Text, txtYeniYazar.Text, cmbYeniKategori.Text));
                _sistem.VerileriKaydet(); ListeyiGuncelle();
            };

            Button btnKul = ButonOlustur("👥 Üyeler", 10, 80, Color.Teal); btnKul.Width = 140; btnKul.ForeColor = Color.White;
            btnKul.Click += (s, e) => { KullaniciListesiniGuncelle(); EkraniGoster(pnlKullanicilar); };

            Button btnLog = ButonOlustur("📜 Geçmiş", 160, 80, Color.DimGray); btnLog.Width = 140; btnLog.ForeColor = Color.White;
            btnLog.Click += (s, e) => { LogListesiniGuncelle(); EkraniGoster(pnlLog); };

            Button btnGec = ButonOlustur("🚨 Gecikenler", 310, 80, Color.Crimson); btnGec.Width = 140; btnGec.ForeColor = Color.White;
            btnGec.Click += (s, e) => { GecikenleriGuncelle(); EkraniGoster(pnlGecikenler); };

            Button btnSil = ButonOlustur("🗑️ Seçili Kitabı Sil", 460, 80, Color.IndianRed); btnSil.Width = 220; btnSil.ForeColor = Color.White;
            btnSil.Click += (s, e) => {
                var k = SecilenKitabGetir(dgvKitaplar);
                if (k == null)
                {
                    MessageBox.Show("Lütfen silmek için listeden bir kitap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!k.RaftaMi)
                {
                    MessageBox.Show("DİKKAT: Bu kitap şu an bir üyede! İade edilmeyen kitaplar sistemden silinemez.", "Güvenlik İhlali", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var onay = MessageBox.Show($"'{k.Ad}' adlı kitabı kalıcı olarak silmek istediğinize emin misiniz?", "Kalıcı Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (onay == DialogResult.Yes)
                {
                    _sistem.Kitaplar.Remove(k);
                    _sistem.VerileriKaydet();
                    ListeyiGuncelle();
                    MessageBox.Show("Kitap sistemden başarıyla silindi.", "Silindi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            pnlAdminPaneli.Controls.AddRange(new Control[] { new Label { Text = "Yönetici Paneli", Location = new Point(10, 5) }, txtYeniKitapAdi, txtYeniYazar, cmbYeniKategori, btnAdd, btnKul, btnLog, btnGec, btnSil });

            // --- PROFIL PANELI ---
            pnlProfil.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => { ListeyiGuncelle(); EkraniGoster(pnlAnaEkran); }));
            Label lblPrfT = new Label { Text = "Hesabım ve Kitaplarım", Location = new Point(270, 20), Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true };
            dgvProfilKitaplar = GridOlustur(20, 60, 690, 200);

            dgvProfilKitaplar.DataBindingComplete += (s, e) => {
                bool dark = _sistem.IsKaranlikMod;
                foreach (DataGridViewRow row in dgvProfilKitaplar.Rows)
                {
                    if (Convert.ToBoolean(row.Cells["GeciktiMi"].Value))
                    {
                        row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(120, 30, 30) : Color.MistyRose;
                        row.DefaultCellStyle.ForeColor = dark ? Color.White : Color.Red;
                    }
                }
            };

            Button btnProfilIade = ButonOlustur("SEÇİLİ KİTABI İADE ET", 20, 270, Color.IndianRed); btnProfilIade.Width = 200; btnProfilIade.ForeColor = Color.White;
            btnProfilIade.Click += (s, e) => {
                if (dgvProfilKitaplar.CurrentRow != null)
                {
                    int id = (int)dgvProfilKitaplar.CurrentRow.Cells["Id"].Value;
                    var k = _sistem.Kitaplar.First(x => x.Id == id);
                    if (_sistem.KitapIadeEt(k)) { MessageBox.Show("İade edildi."); ProfilKitaplariniGuncelle(); }
                }
                else MessageBox.Show("Kitap seçin.");
            };

            lblProfilBilgi = new Label { Location = new Point(20, 320), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            txtEskiSifre = TextboxOlustur("Eski Şifre", 20, 360, true); txtYeniSifre = TextboxOlustur("Yeni Şifre", 230, 360, true);
            Button btnSifU = ButonOlustur("Güncelle", 440, 355, Color.Orange); btnSifU.Width = 120;
            btnSifU.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtYeniSifre.Text)) MessageBox.Show("Yeni şifre boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (_sistem.AktifUye.Sifre != txtEskiSifre.Text) MessageBox.Show("Eski şifre hatalı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else { _sistem.AktifUye.Sifre = txtYeniSifre.Text; _sistem.VerileriKaydet(); MessageBox.Show("Başarılı!"); txtEskiSifre.Clear(); txtYeniSifre.Clear(); }
            };
            pnlProfil.Controls.AddRange(new Control[] { lblPrfT, dgvProfilKitaplar, btnProfilIade, lblProfilBilgi, txtEskiSifre, txtYeniSifre, btnSifU });

            // --- KULLANICILAR PANELI ---
            pnlKullanicilar.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => EkraniGoster(pnlAnaEkran)));
            dgvKullanicilar = GridOlustur(20, 60, 690, 550); pnlKullanicilar.Controls.Add(dgvKullanicilar);

            // --- LOG PANELI ---
            pnlLog.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => EkraniGoster(pnlAnaEkran)));
            dgvLoglar = GridOlustur(20, 60, 690, 550); pnlLog.Controls.AddRange(new Control[] { new Label { Text = "İşlem Geçmişi", Location = new Point(300, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold) }, dgvLoglar });

            // --- GECİKENLER PANELI ---
            pnlGecikenler.Controls.Add(SolUstGeriButonuOlustur("Geri", (s, e) => EkraniGoster(pnlAnaEkran)));
            dgvGecikenler = GridOlustur(20, 60, 690, 550);
            pnlGecikenler.Controls.AddRange(new Control[] { new Label { Text = "Gecikenler", Location = new Point(250, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold) }, dgvGecikenler });

            pnlAnaEkran.Controls.AddRange(new Control[] { lblHosgeldin, btnPrf, lblSaat, txtArama, cmbKategoriler, dgvKitaplar, btnO, btnI, pnlAdminPaneli });
            this.Controls.AddRange(new Control[] { pnlBaslangic, pnlKayit, pnlGiris, pnlAnaEkran, pnlProfil, pnlKullanicilar, pnlLog, pnlGecikenler });
        }

        private void ProfilKitaplariniGuncelle()
        {
            var benimKitaplar = _sistem.Kitaplar.Where(k => k.OduncAlanUyeId == _sistem.AktifUye.Id).Select(k => {
                int gun = (DateTime.Now - k.OduncAlinmaTarihi!.Value).Days;
                string durumTxt = gun <= 15 ? $"{gun} Gündür Sizde" : $"⚠️ {gun - 15} Gün Gecikti! (Ceza: {(gun - 15) * 5} TL)";
                return new { k.Id, Kitap = k.Ad, Yazar = k.Yazar, Durum = durumTxt, GeciktiMi = gun > 15 };
            }).ToList();

            dgvProfilKitaplar.DataSource = null; dgvProfilKitaplar.DataSource = benimKitaplar;
            if (dgvProfilKitaplar.Columns["Id"] != null) dgvProfilKitaplar.Columns["Id"].Visible = false;
            if (dgvProfilKitaplar.Columns["GeciktiMi"] != null) dgvProfilKitaplar.Columns["GeciktiMi"].Visible = false;
            lblProfilBilgi.Text = $"Toplam kayıtlı kitap: {benimKitaplar.Count}";
        }

        private void GecikenleriGuncelle()
        {
            var gecikenler = _sistem.Kitaplar.Where(k => !k.RaftaMi && (DateTime.Now - k.OduncAlinmaTarihi!.Value).Days > 15).Select(k => {
                var uye = _sistem.Uyeler.FirstOrDefault(u => u.Id == k.OduncAlanUyeId);
                int gun = (DateTime.Now - k.OduncAlinmaTarihi!.Value).Days - 15;
                return new { Kitap = k.Ad, Üye = uye != null ? uye.KullaniciAdi : "Bilinmiyor", Gecikme = $"{gun} Gün", Ceza = $"{gun * 5} TL" };
            }).ToList();
            dgvGecikenler.DataSource = null; dgvGecikenler.DataSource = gecikenler;
        }

        private void LogListesiniGuncelle() { dgvLoglar.DataSource = null; dgvLoglar.DataSource = _sistem.Gecmis.OrderByDescending(x => x.Tarih).ToList(); }

        private void TemayiUygula()
        {
            bool dark = _sistem.IsKaranlikMod;
            Color bg = dark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            Color fg = dark ? Color.White : Color.Black;
            Color pnlBg = dark ? Color.FromArgb(45, 45, 48) : Color.WhiteSmoke;
            this.BackColor = bg;
            if (dark) { btnTemaSec.Text = "🌙"; btnTemaSec.ForeColor = Color.LightGray; } else { btnTemaSec.Text = "☀️"; btnTemaSec.ForeColor = Color.Gold; }

            foreach (Control p in this.Controls) if (p is Panel)
            {
                p.BackColor = pnlBg;
                foreach (Control c in p.Controls)
                {
                    if (c is Label) c.ForeColor = fg;
                    if (c is TextBox || c is ComboBox) { c.BackColor = dark ? Color.FromArgb(60, 60, 60) : Color.White; c.ForeColor = fg; }
                    if (c is Button btn && btn != btnTemaSec)
                    {
                        if (btn.Name == "btnPrf") { btn.BackColor = dark ? Color.FromArgb(0, 150, 180) : Color.Gainsboro; btn.ForeColor = dark ? Color.White : Color.Black; }
                        else if (btn.Tag is Color oc) { btn.BackColor = dark ? Color.FromArgb(oc.R / 2, oc.G / 2, oc.B / 2) : oc; btn.ForeColor = Color.White; }
                    }
                    if (c is DataGridView dgv)
                    {
                        dgv.BackgroundColor = dark ? Color.FromArgb(35, 35, 35) : Color.White; dgv.DefaultCellStyle.BackColor = dark ? Color.FromArgb(45, 45, 45) : Color.White;
                        dgv.DefaultCellStyle.ForeColor = fg; dgv.ColumnHeadersDefaultCellStyle.BackColor = dark ? Color.DarkSlateGray : Color.LightGray;
                        dgv.ColumnHeadersDefaultCellStyle.ForeColor = dark ? Color.White : Color.Black; dgv.EnableHeadersVisualStyles = false;
                    }
                }
            }
            if (_sistem.AktifUye != null) { ListeyiGuncelle(); ProfilKitaplariniGuncelle(); }
        }

        private void ListeyiGuncelle()
        {
            string ar = txtArama.Text.ToLower(); string kt = cmbKategoriler.Text;
            var res = _sistem.Kitaplar.Where(k => (kt == "Tümü" || k.Kategori == kt) && (k.Ad.ToLower().Contains(ar) || k.Yazar.ToLower().Contains(ar))).Select(k => new { k.Id, k.Ad, k.Yazar, k.Kategori, Durum = k.DurumBilgisiGetir(_sistem.AktifUye, _sistem.Uyeler) }).ToList();
            dgvKitaplar.DataSource = null; dgvKitaplar.DataSource = res;
        }

        private void KullaniciListesiniGuncelle() { dgvKullanicilar.DataSource = null; dgvKullanicilar.DataSource = _sistem.Uyeler.Select(u => new { u.Id, u.KullaniciAdi, Rol = u.IsAdmin ? "Admin" : "Üye", Kitaplar = _sistem.Kitaplar.Count(k => k.OduncAlanUyeId == u.Id) }).ToList(); }
        private Kitap SecilenKitabGetir(DataGridView dgv) { if (dgv.CurrentRow == null) return null!; int id = (int)dgv.CurrentRow.Cells["Id"].Value; return _sistem.Kitaplar.First(k => k.Id == id); }
        private Panel PanelOlustur() => new Panel { Dock = DockStyle.Fill, Visible = false };
        private DataGridView GridOlustur(int x, int y, int w, int h) => new DataGridView { Location = new Point(x, y), Size = new Size(w, h), ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None };
        private Button ButonOlustur(string t, int x, int y, Color c) { Button b = new Button { Text = t, Location = new Point(x, y), Size = new Size(200, 40), BackColor = c, FlatStyle = FlatStyle.Flat, Tag = c }; b.FlatAppearance.BorderSize = 0; return b; }
        private TextBox TextboxOlustur(string p, int x, int y, bool isPass = false) => new TextBox { PlaceholderText = p, Location = new Point(x, y), Width = 200, PasswordChar = isPass ? '*' : '\0' };
        private Button SolUstGeriButonuOlustur(string y, EventHandler ev) { Button b = new Button { Text = "🔙 " + y, Location = new Point(10, 10), Size = new Size(80, 30), Tag = Color.LightGray, BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat }; b.Click += ev; return b; }
        private void EkraniGoster(Panel p) { foreach (Control c in this.Controls) if (c is Panel) c.Visible = false; p.Visible = true; }
    }
}