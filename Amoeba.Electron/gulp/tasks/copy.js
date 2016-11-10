// @file copy.js
var gulp = require('gulp');
var config = require('../config').copy;

gulp.task('copy', function () {
    for(var i = 0; i < config.length; i++){
        gulp.src(config[i].src, config[i].options)
            .pipe(gulp.dest(config[i].dest));
    }
});