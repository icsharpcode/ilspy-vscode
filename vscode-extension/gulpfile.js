'use strict'

const gulp = require('gulp');
const tslint = require('gulp-tslint');

gulp.task('tslint', () => {
    return gulp.src([
        '**/*.ts',
        '!**/*.d.ts',
        '!**/typings**',
        '!node_modules/**',
        '!vsix/**'
    ])
        .pipe(tslint({
            program: require('tslint').Linter.createProgram("./tsconfig.json"),
            rulesDirectory: "node_modules/tslint-microsoft-contrib",
            configuration: "./tslint.json"
        }))
        .pipe(tslint.report({
            summarizeFailureOutput: false,
            emitError: false
        }));
});
