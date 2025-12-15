var wrapper = document.getElementById("signature-pad"),
    //clearButton = wrapper.querySelector("[data-action=clear]"),
    saveButton = wrapper.querySelector("[data-action=save]"),
    canvas1 = wrapper.querySelector("[data-action=canvas1]"),
    canvas2 = wrapper.querySelector("[data-action=canvas2]"),
    signaturePad1,
    signaturePad2;

// Adjust canvas coordinate space taking into account pixel ratio,
// to make it look crisp on mobile devices.
// This also causes canvas to be cleared.
function resizeCanvas() {
    // When zoomed out to less than 100%, for some very strange reason,
    // some browsers report devicePixelRatio as less than 1
    // and only part of the canvas is cleared then.
    var ratio = Math.max(window.devicePixelRatio || 1, 1);
    canvas1.width = canvas1.offsetWidth * ratio;
    canvas1.height = canvas1.offsetHeight * ratio;
    canvas1.getContext("2d").scale(ratio, ratio);

    canvas2.width = canvas2.offsetWidth * ratio;
    canvas2.height = canvas2.offsetHeight * ratio;
    canvas2.getContext("2d").scale(ratio, ratio);
}

window.onresize = resizeCanvas;
resizeCanvas();

signaturePad1 = new SignaturePad(canvas1);
signaturePad2 = new SignaturePad(canvas2);

//clearButton.addEventListener("click", function (event) {
//    signaturePad1.clear();
//    signaturePad2.clear();
//});

saveButton.addEventListener("click", function (event) {
    if (signaturePad1.isEmpty()) {
        //alert("Please provide signature1 first.");
    } else {
        document.getElementById("hfSign1").value = signaturePad1.toDataURL();
    }
    if (signaturePad2.isEmpty()) {
        //alert("Please provide signature2 first.");
    } else {
        document.getElementById("hfSign2").value = signaturePad2.toDataURL();
    }
});