/*
$(document).ready(function() {
	$("h2").click(function() {
		$(".customer").slideToggle();
	});
});

$(".accod-content").css("background-color", "#96c8da");


*/

$(document).ready(function() {

	$(".accod-head").click(function() {
		$(".accod-content").hide().prev(".accod-head").removeClass("accod-head-select");
		$(this).next(".accod-content").show().prev(".accod-head").addClass("accod-head-select");
	});
	
	$(".customer-head").click(function() {
		$(".customer-content").slideToggle();
	});
	
	$(".work-order-details-head").click(function () {
		$(".work-order-details-content").slideToggle();
	});

	$(".work-requested-details-head").click(function () {
	    $(".work-requested-details-content").slideToggle();
	});

	$(".parts-ordering-details-head").click(function () {
	    $(".parts-ordering-details-content").slideToggle();
	});

	$(".erf-details-head").click(function () {
	    $(".erf-details-content").slideToggle();
	});

	$(".notes-details-head").click(function () {
	    $(".notes-details-content").slideToggle();
	});

	$(".work-order-dispatch-head").click(function () {
	    $(".work-order-dispatch-content").slideToggle();
	});

	$(".closure-head").click(function () {
	    $(".closure-content").slideToggle();
	});

    $(".billable-head").click(function () {
        $(".billable-content").slideToggle();
    });
   
	$(".know-equipment-details-head").click(function () {
	    $(".know-equipment-details-content").slideToggle();
	});
	
	$(".work-performed-head").click(function() {
		$(".work-performed-content").slideToggle();
	});
	
	$(".parts-ordering-head").click(function() {
		$(".parts-ordering-content").slideToggle();
	});
	
	$(".load-van-head").click(function() {
		$(".load-van-content").slideToggle();
	});
	
	$(".known-equipment-head").click(function() {
		$(".known-equipment-content").slideToggle();
	});
	
	$(".work-order-history-head").click(function() {
		$(".work-order-history-content").slideToggle();
	});
});






