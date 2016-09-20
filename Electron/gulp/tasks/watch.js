// @file watch.js
var gulp = require('gulp');
var watch = require('gulp-watch');
var config = require('../config').watch;

gulp.task('watch', function () {
    gulp.start(['webpack']);
    gulp.start(['copy']);

    watch(config.js, function () {
        gulp.start(['webpack']);
    });

    watch(config.www, function () {
        gulp.start(['copy']);
    });
});