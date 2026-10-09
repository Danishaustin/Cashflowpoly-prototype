using UnityEngine.UIElements;

public partial class UIManager
{
    // Keadaan tombol dibedakan dengan kelas, bukan dengan mengganti background-image dari kode,
    // supaya kedua ikonnya tetap tertulis di USS dan dapat disetel dari sana.
    private const string PasswordVisibleClass = "password-terlihat";

    private Button passwordToggleButton;
    private Button addPlayerPasswordToggleButton;

    // Tombol dan pembungkusnya ada di Home.uxml; di sini hanya perilakunya yang dipasang.
    // Posisinya diatur USS: .password-toggle berposisi mutlak terhadap .password-row, pembungkus
    // yang memuat kotak isian beserta tombolnya. Di UI Toolkit elemen berposisi mutlak mengacu pada
    // induk langsungnya, jadi pembungkus itulah yang memberi acuan tinggi dan tepi kanan.
    private void SetupPasswordToggles()
    {
        passwordToggleButton = BindPasswordToggle("PasswordToggleButton", passwordInput);
        addPlayerPasswordToggleButton =
            BindPasswordToggle("AddPlayerPasswordToggleButton", addPlayerPasswordInput);
    }

    private Button BindPasswordToggle(string buttonName, TextField field)
    {
        Button toggle = rootElement?.Q<Button>(buttonName);
        if (toggle == null || field == null)
        {
            return null;
        }

        toggle.clicked += () =>
        {
            field.isPasswordField = !field.isPasswordField;
            toggle.EnableInClassList(PasswordVisibleClass, !field.isPasswordField);
        };

        return toggle;
    }

    // Kata sandi dikembalikan ke keadaan tersembunyi setiap kali layarnya ditinggalkan, supaya tidak
    // tertinggal terbuka di layar berikutnya atau saat orang lain memakai perangkat yang sama.
    private void ResetPasswordToggles()
    {
        ResetPasswordToggle(passwordInput, passwordToggleButton);
        ResetPasswordToggle(addPlayerPasswordInput, addPlayerPasswordToggleButton);
    }

    private void ResetPasswordToggle(TextField field, Button toggle)
    {
        if (field != null)
        {
            field.isPasswordField = true;
        }

        toggle?.RemoveFromClassList(PasswordVisibleClass);
    }
}
