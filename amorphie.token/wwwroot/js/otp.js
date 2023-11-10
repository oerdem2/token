var otpButton = document.getElementById("otp-submit");
var otpForm = document.getElementById('otp-form');
otpButton.addEventListener('click',function(e){
    otpForm.submit();
});