$(function () {
    var hub = $.connection.crosswordAnagramHub;

    hub.client.OnSearchStarted = function () {
        $("#totalPermutations").text("Started");
        $("#bestGuesses").empty();
        $("#topAnswers").empty();
        $("#percentageComplete").empty();    
    };

    hub.client.OnPermutationCount = function (count)
    {
        $("#totalPermutations").text("Permutation Count " + count);    
    };

    hub.client.OnPercentageComplete  = function (percentComplete, expectedFinishTime)
    {
        $("#percentageComplete").text(percentComplete + "% complete, expected time remaining " + expectedFinishTime);
    };

    hub.client.OnBestGuesses  = function (guesses)
    {        
        $("#topAnswers").empty();
        $("#bestGuesses").empty();

        $("#bestGuesses").append("<div>Best Guesses</div>");
        for (var i = 0; i < guesses.length; i++) {
            $("#bestGuesses").append("<div>" + guesses[i] + "</div>");
        }        
    };

    hub.client.OnSearchComplete = function (topAnswers)
    {
        $("#topAnswers").empty();
        $("#bestGuesses").empty();

        $("#topAnswers").append("<div>Top Answers</div>");
        for (var i = 0; i < topAnswers.length; i++) {
            $("#topAnswers").append("<div>" + topAnswers[i] + "</div>");
        }
    };

    $.connection.hub.start().done(function () {
        $('#search').click(function () {
            hub.server.search($('#clue').val(), $('#availableLetters').val());
        });

    });
});