\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{uit-thesis}[2014/16/02 v0.1a UiT thesis class]


%:----------------- INFO, WARNING, ERROR -----------------

\newcommand\ult@info[1]{%
  \ClassInfo{uit-thesis}{#1}%
}

\newcommand\ult@warning[1]{%
  \ClassWarning{uit-thesis}{#1}%
}

\newcommand\ult@warning@nl[1]{%
  \ClassWarningNoLine{uit-thesis}{#1}%
}

\newcommand\ult@critical@error[1]{%
  \ClassError{uit-thesis}{#1}{\@ehd}%
}


%:-------------------------- DEBUG -----------------------

% Used to halt LaTeX compilation
\def\debughalt#1{%
  \PackageError{uit-thesis}{DEBUG}{#1}}

% Used to output definition of a LaTeX macro in console
\def\debugdef#1{%
  \typeout{[DEBUG] Definition of \@backslashchar #1: \expandafter\meaning\csname#1\endcsname ^^J ^^J}}


%:-------------------------- Early includes and checks -----------------------

% e-Tex tools, macros, hooks, etc.
\RequirePackage{etex}
\RequirePackage{etoolbox}

% Used to provide \ifpdf macro
\RequirePackage{ifpdf}

\ifpdf\else
  \ult@critical@error{The uit-thesis template only supports compiling with pdflatex}
\fi

\newif\ifult@has@lthooks
\ult@has@lthooksfalse
\ifcsname DeclareHookRule\endcsname%
  % LaTeX2e 2020-10-01 or newer with lthooks system
  \ult@has@lthookstrue
\fi%


%:-------------------------- Basic class options -----------------------

% We're based on the standard 'book' class
\def\baseclass{book}

\DeclareOption*{\PassOptionsToClass{\CurrentOption}{\baseclass}}

\def\@checkoptions#1#2{
  \edef\@curroptions{\@ptionlist{\@currname.\@currext}}
  \@tempswafalse
  \@tfor\@this:=#2\do{
    \@expandtwoargs\in@{,\@this,}{,\@curroptions,}
    \ifin@ \@tempswatrue \@break@tfor \fi}
  \let\@this\@empty
  \if@tempswa \else \PassOptionsToClass{#1}{\baseclass}\fi
}

\@checkoptions{11pt}{{10pt}{11pt}{12pt}}

% Option overrides
\PassOptionsToClass{a4paper}{\baseclass}
\PassOptionsToClass{oneside}{\baseclass}

% Class options for whether to produce a PDF for printing (A4 with 3mm bleed)
% or for viewing (normal A4 dimensions)
\newif\ifult@print
\newif\ifult@print@specified
\ult@print@specifiedfalse % Default to false
\DeclareOption{print}{\ult@print@specifiedtrue\ult@printtrue}
\DeclareOption{noprint}{\ult@print@specifiedtrue\ult@printfalse}

% Suppress book class options
\DeclareOption{a5paper}{}
\DeclareOption{b5paper}{}
\DeclareOption{letterpaper}{}
\DeclareOption{legalpaper}{}
\DeclareOption{executivepaper}{}
\DeclareOption{landscape}{}
\DeclareOption{openany}{}
\DeclareOption{twocolumn}{}

% Option for drawing baseline grid
\newif\ifult@drawbaselinegrid
\ult@drawbaselinegridfalse % Default to false
\DeclareOption{baselinegrid}{\ult@drawbaselinegridtrue}

\newif\ifult@copyright@disabled
\ult@copyright@disabledfalse % Default to false
\DeclareOption{nocopyright}{\ult@copyright@disabledtrue}

% Option for using indented paragraphs instead of vertical skip between paragraphs
\newif\ifult@parindent
\ult@parindentfalse % Default to false
\DeclareOption{parindent}{\ult@parindenttrue}

% Option for resetting expansion of acronyms at start of each chapter
\newif\ifult@resetallglsperchapter
\ult@resetallglsperchapterfalse % Default to false
\DeclareOption{glsresetperchapter}{\ult@resetallglsperchaptertrue}

% Option for ignoring drawing of List of Figures
\newif\ifult@printlistoffigures
\ult@printlistoffigurestrue % Default to true
\DeclareOption{notlof}{\ult@printlistoffiguresfalse}

% Option for ignoring drawing of List of Tables
\newif\ifult@printlistoftables
\ult@printlistoftablestrue % Default to true
\DeclareOption{notlot}{\ult@printlistoftablesfalse}

% Extra options for glossaries package
\newcommand*{\ult@glossariesextraoptions}{,}
\DeclareOption{xindy}{\gappto\ult@glossariesextraoptions{xindy,}}

% Detect if USenglish is specified in the class options
\newif\ifult@specified@english
\ult@specified@englishfalse
\DeclareOption{USenglish}{\ult@specified@englishtrue}

% Now, process!
\ProcessOptions\relax
\LoadClass{\baseclass}

\ifult@print@specified\else
  \ult@critical@error{%
    You must specify either the 'print' or 'noprint'^^J%
    document class option.^^J%
    ^^J%
    Use \@backslashchar documentclass[print]{uit-thesis} when you want to produce a PDF for^^J%
    "professional grade" printing. This will use a page size of 216mm x 303mm^^J%
    (corresponding to A4 with an additional 3mm bleed added to all of the paper^^J%
    edges), to facilitate proper printing of front and back page artwork that^^J%
    extends to the edges of the paper. Note that using the 'print' option requires^^J%
    the use of 216mm x 303mm paper, and a printer that can support this paper size.^^J%
    ^^J%
    Use \@backslashchar documentclass[noprint]{uit-thesis} when you want to produce a PDF with^^J%
    normal A4 size, for example for electronic publishing, or when you want to^^J%
    print the document using a normal "consumer grade" printer%
  }%
\fi

% Lengths for paper width and height, and horizontal and vertical bleed
\newlength{\ult@paper@width}
\newlength{\ult@paper@height}
\newlength{\ult@paper@hbleed}
\newlength{\ult@paper@vbleed}
\newlength{\ult@coverpages@graphics@width}
\newlength{\ult@coverpages@graphics@height}
\newlength{\ult@coverpages@graphics@hoffset}
\newlength{\ult@coverpages@graphics@voffset}

% Set the appropriate lengths for the cover pages (front page and back page), depending on
% whether the PDF is produced for printing or not
\ifult@print
  \setlength{\ult@paper@hbleed}{3mm}
  \setlength{\ult@paper@vbleed}{3mm}

  \setlength{\ult@coverpages@graphics@hoffset}{\z@}
  \setlength{\ult@coverpages@graphics@voffset}{\z@}
\else
  \setlength{\ult@paper@hbleed}{\z@}
  \setlength{\ult@paper@vbleed}{\z@}

  \setlength{\ult@coverpages@graphics@hoffset}{3mm}
  \setlength{\ult@coverpages@graphics@voffset}{3mm}
\fi

% Calculate the paper size (A4 paper size + bleed depending on whether print option is set or not)
\setlength{\ult@paper@width}{210mm}
\addtolength{\ult@paper@width}{2\ult@paper@hbleed}
\setlength{\ult@paper@height}{297mm}
\addtolength{\ult@paper@height}{2\ult@paper@vbleed}

% Dimensions for cover page graphics
\setlength{\ult@coverpages@graphics@width}{216mm}
\setlength{\ult@coverpages@graphics@height}{303mm}

% Front page left margin
\newlength{\ult@frontpage@leftmargin}
\setlength{\ult@frontpage@leftmargin}{27mm}

% Front page right margin
\newlength{\ult@frontpage@rightmargin}
\setlength{\ult@frontpage@rightmargin}{12mm}

% Front page top margin
\newlength{\ult@frontpage@topmargin}
\setlength{\ult@frontpage@topmargin}{12mm}

% Front page bottom margin
\newlength{\ult@frontpage@bottommargin}
\setlength{\ult@frontpage@bottommargin}{12mm}

% Front page text box horizontal position
\newlength{\ult@frontpage@textbox@xpos}
\setlength{\ult@frontpage@textbox@xpos}{\z@}
\addtolength{\ult@frontpage@textbox@xpos}{\ult@paper@hbleed}
\addtolength{\ult@frontpage@textbox@xpos}{\ult@frontpage@leftmargin}

% Front page text box vertical position
\newlength{\ult@frontpage@textbox@ypos}
\setlength{\ult@frontpage@textbox@ypos}{\z@}
\addtolength{\ult@frontpage@textbox@ypos}{\ult@paper@vbleed}
\addtolength{\ult@frontpage@textbox@ypos}{\ult@frontpage@topmargin}
\addtolength{\ult@frontpage@textbox@ypos}{67mm}

% Front page graphics vertical position
\newlength{\ult@frontpage@graphics@ypos}
\setlength{\ult@frontpage@graphics@ypos}{\ult@paper@height}
\addtolength{\ult@frontpage@graphics@ypos}{-\ult@paper@vbleed}
\addtolength{\ult@frontpage@graphics@ypos}{-12mm}

% Inject USenglish in front of class options if it was not specified in the list.
% If any other language is specified in the list, it will take precedence
% We only inject if it was not specified, since every option is only processed once.
\ifult@specified@english\else
\preto\@classoptionslist{USenglish,}
\fi


%:-------------------------- Require packages -----------------------

% Used to suppress certain package warnings
\RequirePackage[safe]{silence}

\WarningFilter*{microtype}{You are using the `ragged2e' package}

% Allow UTF-8 characters in .tex source files (e.g. so we don't have to escape Norwegian characters)
\RequirePackage[utf8]{inputenc}

% Support text copy and text search in LaTex pdf documents with ligatures (e.g. ff, fi, ...)
\RequirePackage[resetfonts]{cmap}

% Correct font encoding / language support in output (hyphenations, etc.)
\RequirePackage[T1]{fontenc}

% American patterns
\RequirePackage[english=usenglishmax]{hyphsubst}
\RequirePackage{babel}

% To modify the heading styles more thoroughly use the titlesec package
\RequirePackage[explicit]{titlesec}
\RequirePackage{sectsty}

% Scalable Computer Modern font support
\RequirePackage{lmodern}
\RequirePackage{type1cm}

% Better support fraction calculations
\RequirePackage{xintexpr}


%:-------------------------- LaTeX control flow macros -----------------------

% Used to provide if-then-else macros
\usepackage{ifthen}

% Allow calculations in \vspace*
\usepackage{calc}

% Used to calculate ratio between two dimension commands
\newcommand*{\DivideLengths}[2]{%
  \xintthe\xintiexpr #1/#2\relax
}

% Used to prevent given LaTeX macro from being called more than once
\newcommand\ult@once[1]{%
  % Save original macro
  \expandafter\let\csname ult@once@save\expandafter\@gobble\string#1\endcsname#1\relax
  %
  % DEBUG
  %\typeout{DEBUG \expandafter\meaning\csname ult@once@savelistoffigures\endcsname}
  %\debughalt{}
  %
  % Re-define macro
  \expandafter\renewcommand\csname\expandafter\@gobble\string#1\endcsname{%
    % Invoke original macro
    \csname ult@once@save\expandafter\@gobble\string#1\endcsname
    % Re-define macro
    \expandafter\renewcommand\csname\expandafter\@gobble\string#1\endcsname{%
      % Warn on second usage
      \ult@warning{Call to \@backslashchar\expandafter\@gobble\string#1\space was ignored, because it was called previously}%
      % Re-define macro
      \expandafter\renewcommand\csname\expandafter\@gobble\string#1\endcsname{%
        % Error on third usage
        \ult@critical@error{Duplicate call to \@backslashchar\expandafter\@gobble\string#1}%
      }%
    }%
  }%
}

% Used to prevent package from being loaded.
% Macro is used as \ult@disallowpackage{packagename} or \ult@disallowpackage[Reason for disallowing package]{packagename}.
\newcommand\ult@disallowpackage[2][\@empty]{%
  \expandafter\edef\csname ult@disallowpackage@#2\endcsname{#1}%
  \AtBeginDocument{%
    \@ifpackageloaded{#2}{%
      \ult@critical@error{Package #2 has been loaded even if it was disallowed}%
    }{}%
  }%
}

% Disallowed packages
\ult@disallowpackage[Pacakge notoccite should not be loaded. The uit-thesis template already integrates the changes from notoccite.]{notoccite}
\ult@disallowpackage[Package subfigure is deprecated. The subcaption package (already included by the document class) is used instead.]{subfigure}
\ult@disallowpackage[Package subfig is incompatible with the uit-thesis document class. The subcaption package (already included) is used instead.]{subfig}
\ult@disallowpackage[Package easyeqn is incompatible with hyperref.]{easyeqn}
\ult@disallowpackage[Package mathenv is incompatible with hyperref.]{mathenv}
\ult@disallowpackage[Package pstcol is not allowed. The uit-thesis document class uses the xcolor package instead.]{pstcol}
\ult@disallowpackage[The uit-thesis document class already provides beautiful chapter headings. Why would you want to change them?]{fncychap}
\ult@disallowpackage[The uit-thesis document class is not compatdxble with KOMA-Script.]{tocbasic}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{tocstyle}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{typearea}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{titlepage}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrpage}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrpage2}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrdate}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrtime}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scraddr}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrextend}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrbase}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrlfile}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrwfile}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrhack}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrlayer}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrlayer-scrpage}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrlayer-notecolumn}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrjura}
\ult@disallowpackage[The uit-thesis document class is not compatible with KOMA-Script.]{scrlogo}
\ult@disallowpackage[The uit-thesis document class uses the float package, which isn't compatible with floatrow.]{floatrow}


%:-------------------------- Colors -----------------------

% Text coloring, table colors, etc.
% Note: the xcolor package will pretend that the color package has been loaded, to prevent the color package
% from being loaded as well. Same goes for the colortbl package.
\usepackage[dvipsnames,table,hyperref]{xcolor}

% Text highlighting
\usepackage{soul}

%% UiT colors (from https://uit.no/ansatte/grafiskprofil#innhold_617372, https://uit.no/ressurs/uit/profil2019/examples/fargekart.png)
\definecolor{pms2189}{cmyk}{1.00,0.17,0,0.73}
\definecolor{pms633}{cmyk}{0.99,0.03,0.16,0.19}
\definecolor{pms2227}{cmyk}{0.60,0,0.20,0}
\definecolor{pms1797}{cmyk}{0.01,0.87,0.89,0.04}
\definecolor{pms130}{cmyk}{0,0.31,0.90,0}


\definecolor{pms2229}{cmyk}{0.98,0,0.29,0}
\definecolor{pms7707}{cmyk}{0.96,0,0.06,0.42}
\definecolor{pms138}{cmyk}{0.02,0.39,0.96,0.07}
\definecolor{pms5435}{cmyk}{0.32,0.09,0.08,0.07}

% Running head color
\definecolor{rheadcolor}{cmyk}{0.15,0,0,0.8}
\colorlet{chapnumcol}{pms633}


%:-------------------------- URLs -----------------------

% Nicely formatted URLs
% Retain spaces in URLs, and allow line break at hyphens
\usepackage[obeyspaces,hyphens]{url}

% Define a new 'leo' style for the package that will use a smaller font.
% http://www.kronto.org/thesis/tips/url-formatting.html
\def\url@leostyle{%
  \@ifundefined{selectfont}{\def\UrlFont{\sf}}{\def\UrlFont{\small\ttfamily}}%
}
% Now actually use the newly defined style.
\urlstyle{leo}


%:-------------------------- Things we need to do magic -----------------------

% Needed by our black magic hacks below
\usepackage{xparse}



% To read LaTeX macro from file
\usepackage{catchfile}

% Convenient
\usepackage{expl3}
\usepackage{xstring}

% Allows us to do hardcore patching
\usepackage{xpatch}


%:-------------------------- Floats -----------------------

\usepackage{ult-rename}

% Floats (should be loaded before hyperref)
\usepackage{float}

% Note: we load newfloat package before we override \@chapter, because
% newfloat tries really hard to override it, and we don't really want it to.
% Now, it should patch the default \@chapter command of the book class.
% If newfloat was included after we have patched \@chapter, then it would
% patch \addtocontents instead.
% See http://mirrors.ctan.org/macros/latex/contrib/caption/newfloat.pdf
\usepackage{newfloat}

\xapptocmd{\floatname}{%
  \renamedefname{#1}{\@nameuse{fname@#1}}
  \renameautorefname{#1}{\@nameuse{fname@#1}}
}
{}
{\ult@critical@error{Failed to patch \@backslashchar floatname}}


%:-------------------------- Book class overrides -----------------------

% Override \@chapter to get a consistent look of all ToC lists
\def\@chapter[#1]#2{%
  \ifnum \c@secnumdepth >\m@ne
    \if@mainmatter
      \refstepcounter{chapter}%
      \typeout{\@chapapp\space\thechapter.}%
      \addcontentsline{toc}{chapter}%
                {\protect\numberline{\thechapter}#1}%
    \else
      \addcontentsline{toc}{chapter}{#1}%
    \fi
  \else
    \addcontentsline{toc}{chapter}{#1}%
  \fi
  \chaptermark{#1}%
  %\addtocontents{lof}{\protect\addvspace{10\p@}}%
  %\addtocontents{lot}{\protect\addvspace{10\p@}}%
  %
  % Add space between entries belonging to different
  % chapters in ToC lists
  \ult@foralltoclists{\ult@this@toclist@ext}{%
    \addtocontents{\ult@this@toclist@ext}%
                  {\protect\addvspace{\ult@cftbeforechapskip}}%
  }%
  \@makechapterhead{#2}%
  \@afterheading
}


%:-------------------------- Footnotes ----------------------------

\usepackage[norule,%    % Drop the small rule before footnotes
            splitrule,% % If a footnote is split, draw a full width rule above the continued part of the footnote as a visual cue to readers
            bottom%     % Make sure footnotes are pushed to the bottom of the page (instead of being placed after the actual page break)
           ]{footmisc}

% The actual marker symbol (per footnote)
\providecommand*{\thefootnotemark}{\@thefnmark}

% Footnote marker font
\newcommand*{\ftnm@font}{\normalfont}

% Footnote text font
\newcommand*{\ftn@font}{\normalfont}

% Definition of footnote mark in actual footnote
% Currently set to adapt to Chicago style footnotes with non-superior footnote labels and single dot after label.
\def\@@makefnmark{%
  \hbox{{\ftnm@font{\@thefnmark .\space}}}%
}

% Footnote paragraph skip
\def\hangfootparskip{\z@}

% Footnote paragraph indentation
\def\hangfootparindent{2.5em}

% Width of footnote label box
\def\ult@footnotemarkwidth{2.4em}

% Horizontal space between footnotemark and start of text
\def\footnotemargin{\z@}

% Redefine footnote with hanging style, based on above parameters
\long\def\@makefntext#1{%
  \bgroup
  \setbox\@tempboxa\hbox{%
    \ifdim\footnotemargin>\z@
      \hb@xt@\z@{\hb@xt@-\footnotemargin{\hss\@@makefnmark}\hss}%
    \else
      \@@makefnmark
    \fi
  }%
  \@tempdima = \hsize
  \addtolength{\@tempdima}{-\ult@footnotemarkwidth}%
  \addtolength{\@tempdima}{-\footnotemargin}%
  \@tempdimb = \ult@footnotemarkwidth
  \addtolength{\@tempdimb}{\footnotemargin}%
  \parshape \@ne \@tempdimb \@tempdima
  \@setpar{{\@@par}}%
  \leavevmode
  \llap{\box\@tempboxa}%
  \parskip\hangfootparskip\relax
  \parindent\hangfootparindent\relax
  \ftn@font#1%
  \par\egroup
}


%:-------------------------- Figures, graphics, includes, etc.  -----------------------

\usepackage[pdftex,%                       %
            plainpages=false,%             % "Physical" page numbers and logical page numbers are not the same
            pdfpagelabels=false,%          % Don't index pages using logical page numbers in PDF reader (such as ii, iii, iv, ...)
            pdfpagemode={UseOutlines},%    % Show bookmarks
            pdfstartview={FitV},%          % Fit height of page to PDF viewer window
            pdfpagelayout={TwoPageRight},% % Display two pages, odd-numbered pages to the right
            pdfdisplaydoctitle,%           % Display document title instead of filename in title bar
            bookmarks,%                    % Show bookmarks bar when displaying document
            pagebackref=false,%            % No back references inside bibilography.
            hyperindex,%                   %
            hyperfigures%                  %
           ]{hyperref}

% Used for adding figures in thesis
\usepackage[pdftex]{graphicx}

% Make it easy to include PDFs using \includepdf macro. Note that PDF links will be lost when including!
\usepackage{pdfpages}

% Correct a problem with hyperref
\usepackage[all]{hypcap}

\hypersetup{%
  bookmarksopen=true,
  bookmarksnumbered=true,
  breaklinks=true,
  linktocpage,
  colorlinks=false,% Should never be set to true, as it reduces quality of print...
  linkcolor=blue,
  urlcolor=blue,
  citecolor=red,
  anchorcolor=green,
  hidelinks, % Remove color and border
  unicode=true, % Enforce unicode encoding in bookmarks, which is required for samin language to interoperate with hyperref
}

\usepackage{ult-autoref}

% Small, hanging indentation with bold label.
% hypcap option is used to make sure that hyperlink to
% e.g. figure points to the top of the figure rather than
% to the caption.
\usepackage[hang,bf,small,hypcap]{caption}

% Enables adding figures and tables side by side
\usepackage{subcaption}

% For sweet-ass drawings and transformations and shit
\usepackage{tikz}
\usepackage[skins]{tcolorbox}
\usepackage{picture}
\usepackage{eso-pic}


%:-------------------------- References -----------------------

% For some reason, hyperref has let some reference names have first capital letter, whereas others do not.
% Make sure all default reference names use first capital letter.
\renameautorefname[USenglish]{section}{Section}
\renameautorefname[USenglish]{footnote}{Footnote}
\renameautorefname[USenglish]{item}{Item}
\renameautorefname[USenglish]{chapter}{Chapter}
\renameautorefname[USenglish]{subsection}{Subsection}
\renameautorefname[USenglish]{paragraph}{Paragraph}
\renameautorefname[USenglish]{subparagraph}{Subparagraph}
\renameautorefname[USenglish]{FancyVerbLine}{Line}
\renameautorefname[USenglish]{page}{Page}
\renameautorefname[USenglish]{part}{Part}
\renameautorefname[USenglish]{appendix}{Appendix}

% Set autoref definitions for norsk language - none of these are set.
\renameautorefname[norsk]{section}{seksjon}
\renameautorefname[norsk]{footnote}{fotnote}
\renameautorefname[norsk]{item}{element}
\renameautorefname[norsk]{chapter}{kapittel}
\renameautorefname[norsk]{subsection}{underseksjon}
\renameautorefname[norsk]{paragraph}{avsnitt}
\renameautorefname[norsk]{subparagraph}{underavsnitt}
\renameautorefname[norsk]{FancyVerbLine}{Line}
\renameautorefname[norsk]{page}{side}
\renameautorefname[norsk]{equation}{formel}
\renameautorefname[norsk]{table}{tabell}
\renameautorefname[norsk]{figure}{figur}
\renameautorefname[norsk]{lstlisting}{listing}
\renameautorefname[norsk]{part}{del}
\renameautorefname[norsk]{appendix}{vedlegg}


%:-------------------------- Title and subtitle  -----------------------

% Override \title to update pdftitle
\let\ult@savetitle\title
\def\title#1{%
  \ult@savetitle{#1}%
  \hypersetup{pdftitle={#1}}%
}

% Used to hold author name as string
\global\let\ult@author\@empty

% Make sure we get an error if using \maketitle without specifying \author{}, not just warning...
\def\@author{\ult@critical@error{No \noexpand\author given}}

% Override \author to update pdfauthor
\let\ult@saveauthor\author
\def\author#1{%
  \gdef\ult@author{#1}%
  \ult@saveauthor{#1}%
  \hypersetup{pdfauthor={#1}}%
}

% Add macro for subtitle
\def\subtitle#1{\gdef\@subtitle{#1}}


%:-------------------------- Fonts -----------------------

\usepackage{opensans}
\@ifpackagelater{opensans}{2019/06/15}
{%
  \newcommand*{\ult@opensans@TLF@family}{opensans-TLF}
  \newcommand*{\ult@opensans@TOsF@family}{opensans-TOsF}
}%
{%
  \newcommand*{\ult@opensans@TLF@family}{fos}
  \newcommand*{\ult@opensans@TOsF@family}{fosj}
}%

\usepackage[osf]{noto-sans}

\renewcommand*{\sfdefault}{\ult@opensans@TOsF@family}

% Dependencies of XCharter
\usepackage{textcomp}
\usepackage{mweights}
\usepackage{fontaxes}
\usepackage{xkeyval}

% XCharter
\usepackage[scaled=.98,sups]{XCharter}% lining figures in math

% Save font family for XCharter with lining figures
\global\let\ult@fontfamilylfstyle\@empty
\begingroup
\lfstyle
\global\let\ult@fontfamilylfstyle\f@family\relax
\endgroup

% Save font family for XCharter with old style figures
\global\let\ult@fontfamilyosfstyle\@empty
\begingroup
\osfstyle
\global\let\ult@fontfamilyosfstyle\f@family\relax
\endgroup

\newcommand{\ult@setlfstyledefault}{%
  \renewcommand*{\rmdefault}{\ult@fontfamilylfstyle}%
  \normalfont
}

\newcommand{\ult@setosfstyledefault}{%
  \renewcommand*{\rmdefault}{\ult@fontfamilyosfstyle}%
  \normalfont
}

% In his Elements of Typographic Style, Robert Bringhurst recommends that you use the
% same amount of space after an end-of-sentence fullstop as there is between words.
% See http://tex.stackexchange.com/questions/26996/implementing-a-sort-of-tightened-frenchspacing
\frenchspacing

% The csquotes package provides convenient macros for typesetting various kinds of quotations.
\usepackage{csquotes}

\usepackage[activate={true,nocompatibility},final,kerning=true,spacing=true,factor=1100,stretch=10,shrink=10]{microtype}

% this is necessary so that smallcapped italics & bolds also get spaced
\DeclareMicrotypeSet{ultsmallcaps}
   { encoding = {OT1,T1,T2A,LY1,OT4,QX,T5,TS1,EU1,EU2},
     shape    = {sc,si,scit}
   }
\microtypesetup{tracking=ultsmallcaps}

% the default tracking is kind of huge, this sets tracking to 5.5% of an em
%\SetTracking{encoding = *, shape = {sc,si,scit}}{55}

\newif\ifult@microtypeloaded
\AtBeginDocument{%
  \@ifpackageloaded{microtype}%
  {%
    \global\ult@microtypeloadedtrue
  }%
  {%
    \global\ult@microtypeloadedfalse
    \global\newif\ifMT@protrusion
    \global\MT@protrusionfalse
  }%
}

% Allows us to scale font sizes relatively
\usepackage{scalefnt}


%:-------------------------- Listings, tables, etc.  -----------------------

% Listings (e.g. used for code examples)
\usepackage{listings}

% Don't use old style numbers for line numbers or in listing content
\lst@AddToHookAtTop{Init}{\ult@setlfstyledefault}

\usepackage{ult-custom-listings}

% Pretty tables
\usepackage{booktabs}
\usepackage{tabularx}


%:-------------------------- Symbols -----------------------

\usepackage{xspace}
\usepackage{amsmath}
\usepackage{amsthm}
\usepackage[libertine,bigdelims,scaled=1.07]{newtxmath}

% Use oldstyle figures in text
% Note: we have to do this manually instead of using XCharter's \useosf command, since last version of XCharter breaks \useosf
\edef\XCharter@figurestyle{TOsF}\edef\XCharter@altone{1}
\AtEndPreamble{%
  % Note: we use \XCharter@figurestyle here instead of TOsF to allow user to
  % override default figure style in preamble!
  \renewcommand*{\rmdefault}{XCharter-\XCharter@figurestyle}%
  \normalfont
}


%:-------------------------- Page layout -----------------------

\newlength\ult@gm@topmargin
\setlength\ult@gm@topmargin\topmargin

\newlength\ult@gm@textheight
\setlength\ult@gm@textheight\textheight

\newlength\ult@gm@textwidth
\setlength\ult@gm@textwidth\textwidth

% Geometry is needed to set frontpage layout
% Enable option 'showframe' for debugging...
\usepackage[%
  papersize={\ult@paper@width,\ult@paper@height},%
  layout=a4paper,%
  layouthoffset=\ult@paper@hbleed,%
  layoutvoffset=\ult@paper@vbleed,%
  ignorehead,%
  ignorefoot,%
 % oneside,%
  top=\the\topmargin,%
  textheight=\the\textheight,%
  textwidth=\the\textwidth%
]{geometry}

% Calculate differences between geometry dimensions and LaTeX dimensions
\addtolength\ult@gm@topmargin{\ult@gm@topmargin}
\addtolength\ult@gm@topmargin{-\topmargin}

\addtolength\ult@gm@textheight{\ult@gm@textheight}
\addtolength\ult@gm@textheight{-\textheight}

\addtolength\ult@gm@textwidth{\ult@gm@textwidth}
\addtolength\ult@gm@textwidth{-\the\textwidth}

% Update geometry to preserve book class' dimensions
\geometry{top=\the\ult@gm@topmargin,textheight=\the\ult@gm@textheight,textwidth=\the\ult@gm@textwidth}


% Fix head height
\geometry{headheight=32pt}

% Make sure width of running heads is updated when geometry is changed
\gappto\Gm@process{\f@nch@setoffs}
\gappto\Gm@changelayout{\f@nch@setoffs}

% Used to modify layout
\usepackage{layout}

% Used to give adjustwidth environment
\usepackage{changepage}

% Get value of baselineskip of normal-sized font in \ult@normalbaselineskip
\newdimen\ult@normalbaselineskip
\begingroup
\normalsize
\global\ult@normalbaselineskip=\baselineskip
\endgroup

% Save \parindent for current font size
\newlength\ult@saveparindent
\setlength{\ult@saveparindent}{\parindent}

% Begin paragraphs with an empty line rather than an indent
% Note: we use package parskip even if uit-thesis class is loaded with class option 'parindent',
% just to make sure that the parskip package cannot be loaded later and fuck up our layout!
\usepackage[parfill]{parskip}[=v1]

\ifult@parindent
  % Restore the default \parindent
  \setlength{\parindent}{\ult@saveparindent}

  % No skip between paragraphs when using indentation
  \setlength{\parskip}{\z@}
\else
  % Set indent at new paragraph to 0
  \setlength{\parindent}{\z@}

  % Set paragraph skip length to two lines
  % Note: this reads the current value of \baselineskip for the normal font and font size, and stores it in parskip.
  % Changing \baselineskip later will NOT change \parskip
  \setlength{\parskip}{\ult@normalbaselineskip}
\fi

\linespread{1.0}

% Strict linespacing, don't allow LaTeX to put extra spacing between lines
\lineskip=\z@

% Note: this breaks tables, figures, and probably other environments too if set < \z@.
\lineskiplimit=\z@

\renewcommand{\baselinestretch}{1.0}\normalsize

% Spacing for \item lists
% \topsep is vertical spacing before first item
% \parsep is spacing between items
% \itemsep = \z@ <=> add nothing to \parsep between items
\def\@listI{%
  \leftmargin\leftmargini
  \topsep\ult@normalbaselineskip
  \advance\topsep by -\parskip
  \parsep\parskip
  \listparindent\parindent
  \itemsep\z@
}
\let\@listi\@listI
\@listi

% BASELINE GRID
% http://tex.stackexchange.com/questions/150619/drawing-a-background-grid-based-on-linespread-value
\ifult@drawbaselinegrid
\AtBeginShipout{%
  \AtBeginShipoutUpperLeft{%
    \color{red}%
    \ifodd\c@page
      \put(\dimexpr 1in+\oddsidemargin,
           -\dimexpr 1in+\topmargin+\headheight+\headsep+\topskip)%
        {%
          \vtop to\dimexpr\vsize+\ult@normalbaselineskip{%
            \hrule
            \leaders\vbox to\ult@normalbaselineskip{\hrule width\hsize\vfill}\vfill
          }%
        }%
    \else
      \put(\dimexpr 1in+\evensidemargin,
           -\dimexpr 1in+\topmargin+\headheight+\headsep+\topskip)%
        {%
          \vtop to\dimexpr\vsize+\ult@normalbaselineskip{%
            \hrule
            \leaders\vbox to\ult@normalbaselineskip{\hrule width\hsize\vfill}\vfill
          }%
        }%
    \fi
  }%
}
\fi

% For \RaggedRight macro, which is better than \raggedright
\usepackage{ragged2e}


%:-------------------------- Table of Contents (TOC), bibliography, etc. -----------------------

% ToC support for list of figures, list of tables, bibliography, index, etc.
\usepackage[nottoc%     % No entry for ToC in ToC
           ]{tocbibind}

% Styling of actual Table of Contents
\usepackage[titles%     % Retain chapter headers
           ]{tocloft}
\usepackage{apacite}
\bibliographystyle{apacite}



\newif\ifult@first@contentsline
\global\ult@first@contentslinefalse

\let\ult@save@contentsline\contentsline
\renewcommand*{\contentsline}[4]{%
  \ult@save@contentsline{#1}{#2}{#3}{#4}%
  \global\ult@first@contentslinefalse
}

% Citation numbering not starting on table of contents
% Note: we do not use notoccite package, since it overwrites changes that
% parskip does to \@starttoc
\AtBeginDocument{
  \renewcommand*{\@starttoc}[1]{%
    % First element in ToC will be a chapter. Compensate for vertical space
    % added by tocloft, so the first element isn't pushed down.
    % Also, elements in ToC lists that belong to different chapters are separated
    % with spacing, and this spacing will be present before the very first element.
    % Hence, we compensate for both these cases...
    \vspace*{-\ult@cftbeforechapskip}%
    %
    % Make sure spacing is the same with class option 'parindent' and without
    \vspace*{\parskip}%
    %
    \begingroup
      % Disable protrusion locally
      \microtypesetup{protrusion=false}%
      %
      % Add modification from notoccite.
      % Note: this only has effect within the current local group
      % (which is why it works!)
      \@fileswfalse
      %
      % Make sure @ have right catcode before reading input file
      \makeatletter
      %
      % Retain modification from parskip.
      % Note: this is only consistent because we compensate with \vspace*{\parskip} above
      \parskip\z@
      %
      \global\ult@first@contentslinetrue
      %
      % Read auxillary file
      \@input{\jobname.#1}%
    \endgroup
    \if@filesw
      \expandafter\newwrite\csname tf@#1\endcsname
      \immediate\openout \csname tf@#1\endcsname \jobname.#1\relax
    \fi
    \@nobreakfalse
  }
}


%:-------------------------- Header/footer -----------------------

% Used to modify header/footer of pages
\usepackage{fancyhdr}
\usepackage{ult-clearpage}

% Define pagestyle
\pagestyle{fancy}

% Reset all header settings
\fancyhf{}

% strips the crazy head rule
\let\headrule\@empty

\newcommand{\rheadfont}{%
  \fontsize{10pt}{12pt}%
  \usefont{T1}{NotoSans-TOsF}{el}{n}%
  \SetTracking{encoding = *, shape = {sc,si,scit}}{100}%
}
\newcommand{\rheadfmt}[1]{\textsc{\MakeLowercase{#1}}}

% Set pagenumber in the header.
% RO = Right Odd: Page number is set to the right in the header on odd pages
% LE = Left Even: Page number is set to the left in the header on even pages
\fancyhead[RO,LE]{\rheadfont\textcolor{rheadcolor}{\thepage}}

% TOPPTEKST
% Set \leftmark to show to the right in header on even pages.
% This will make sure that chapter title is written to the right on even/verso pages
\fancyhead[RE]{\rheadfont\textcolor{rheadcolor}{\leftmark}}
% Hvis du vil ha UiT og DIPS-logo i topp:
%\fancyhead[RE]{\includegraphics[scale=0.04]{uit-thesis-master/resources/UiT_Segl_Bok_Bla_960px.png} \hspace{0.7\textwidth}
%  \includegraphics[scale=0.1]{uit-thesis-master/resources/dips_logo_payoff.png}\\ \rheadfont\textcolor{rheadcolor}{\leftmark}}

% Redefine what is in \sectionmark so that \rightmark contains section number and section name (e.g. "2.1 My Sections Name")
\renewcommand{\sectionmark}[1]{%
  \markright{\includegraphics[scale=0.04]{uit-thesis-master/resources/UiT_Segl_Bok_Bla_960px.png}\hspace{0.7\textwidth}
  \includegraphics[scale=0.1]{uit-thesis-master/resources/dips_logo_payoff.png}\\ 
  \rheadfmt{\thesection}\hspace{5pt}\printrheadslash\hspace{5pt}\rheadfmt{#1}}% \leftmark (right even) is set to section number + title after start of new section
}

% Redefine "chapter mark" for Toc lists (\tocetcmark command is originally defined in tocbibind...)
% Note: this also applies to running heads for ToC itself, due to the use of \prw@mkboth in our definition later.
\renewcommand{\tocetcmark}[1]{%
  \markboth{\rheadfmt{#1}}% \leftmark
           {\rheadfmt{#1}}% \rightmark
}

% Redefine "plain" pagestyle (used on first page of each chapter)
\fancypagestyle{plain}{%
  \fancyhf{}% clear all header and footer fields
  \fancyfoot[C]{\rheadfont \textcolor{rheadcolor}{\thepage}}% except the center
  \renewcommand{\headrulewidth}{0pt}%
  \renewcommand{\footrulewidth}{0pt}%
}


%:-------------------------- Black magic hacks -----------------------

\usepackage{ult-upsc}

\DeclareRobustCommand{\ult@upsc@fntsetup}{%
  \scalefont{1.12}%
  \microtypesetup{tracking=false}% Disable tracking to reduce letterspacing for acronyms and similar
}

\LetLtxMacro{\ult@upsc}{\upsc}
\renewcommand{\upsc}[1]{%
  \begingroup
    \ult@upsc@fntsetup
    \ult@upsc{#1}%
  \endgroup
}

%:-------------------------- VersionHistory -----------------------

\usepackage[tocentry, owncaptions, nochapter]{vhistory}
\renewcommand{\vhchangename}{Beskrivelse}
\renewcommand{\vhdatename}{Dato}
\renewcommand{\vhauthorname}{Forfatter(e)}
\renewcommand{\vhversionname}{Versjon}


%:-------------------------- Glossaries -----------------------
%Used for abreviations and list of abreviations
%Translate=false because package 'babel' reverts renaming of the default glossary-/acronymnames
\usepackage[toc,acronym,shortcuts,nonumberlist,nopostdot,translate=false,\ult@glossariesextraoptions]{glossaries}
\usepackage{ult-glossaries}


%Apply custom smallcaps glossaries style
\setacronymstyle{ult-long-upsc-short}

\newglossarystyle{ult-list}{%
  base it on the list style
  \setglossarystyle{list}%
  
  \renewenvironment{theglossary}{%
    \begin{adjustwidth}{\cftfigindent}{}% Left align with same indent as LoF
    \begin{description}%
  }{%
    \end{description}%
    \end{adjustwidth}%
  }%
  \renewcommand*{\glsnamefont}[1]{\upsc{##1}}%
}

% Better "long" style (consistent spacing, prevent running headers on empty page)
\newglossarystyle{ult-long}{%
  % base it on the long style
  \setglossarystyle{long}%
  \renewenvironment{theglossary}{%
    % Remove vspace before and after table (note: these are only modified locally)
    \setlength{\LTpre}{0pt}\setlength{\LTpost}{0pt}%
    \setlength\LTleft{\cftfigindent}% Left align with same indent as LoF
    \leavevmode% Make sure table isn't ouput one line before it should
    % Note: {longtable*} is unnumbered, so table-counter isn't increased!
    \begin{longtable*}{@{}l p{\glsdescwidth}}%
  }{%
    \end{longtable*}%
  }%
  \renewcommand*{\glsnamefont}[1]{\textbf{\upsc{##1}}}%
}

\AtBeginDocument{%
  \renewcommand*{\printacronyms}[1][]{%
    \printglossary[type=\acronymtype,style=ult-list,#1]%
  }%
}

\ifult@has@lthooks
  % Ensure our redefinition of \printacronyms happens after the glossaries package has defined it!
  \DeclareHookRule{begindocument}{.}{after}{glossaries}
\fi%

% Prevent grouping together abbreviations starting on same letter in list of
% abbreviations.
% See http://tex.stackexchange.com/questions/4183/package-glossaries-single-spaced-printglossaries
\glsnogroupskiptrue
\renewcommand*{\glsgroupskip}{}

% Make sure running heads of glossaries are output consistently with other ToC lists (using smallcaps)
\renewcommand{\glossarymark}[1]{\prw@mkboth{#1}}


%:-------------------------- Chapter and section style -----------------------

% Alias sectsty internals
\newcommand{\chapnumfont}{\SS@chapnumfont}       % use \chapternumberfont{} to change
\newcommand{\chaptitlefont}{\SS@chaptitlefont}   % use \chaptertitlefont{} to change
\newcommand{\sectfont}{\SS@sectfont}             % use \sectionfont{} to change
\newcommand{\subsectfont}{\SS@subsectfont}       % use \subsectionfont{} to change
\newcommand{\subsubsectfont}{\SS@subsubsectfont} % use \subsubsectionfont{} to change
\newcommand{\parafont}{\SS@parafont}             % use \paragraphfont{} to change

% Use Open Sans as chapter and section title font
\allsectionsfont{\usefont{T1}{\ult@opensans@TOsF@family}{bx}{n}}

% Change font for chapter number
\chapternumberfont{%
  \fontsize{56}{56}%                             % font size 56pt, baselineskip 56pt
  \usefont{T1}{\ult@opensans@TLF@family}{b}{n}%  % Use Open Sans as chapter number font
}

% Draw the UiT slash (must be used inside a tikzpicture environment)
\newcommand{\ult@chapslashtikz}{%
  \fill[color=chapnumcol] (0,0) -- (1.15,3.15) -- (1.4,3.15) -- (0.25,0) -- cycle;
}

% Strutbox for numbers, to make sure that numbers are given correct vertical positions
\newbox\ult@chapnummetabox
\newbox\ult@chapnumbox
\AtBeginDocument{%
  \setbox\ult@chapnummetabox\hbox{\Huge\bfseries\chapnumfont 0123456789}%
  \setbox\ult@chapnumbox\hbox{%
    \vrule\@height\ht\ult@chapnummetabox
          \@depth\dp\ult@chapnummetabox
          \@width\z@
  }%
}

% Define how chapter number is drawn
\newcommand{\printchapternum}[1][]{%
  \begin{tikzpicture}%
    %\tikzstyle{every rectangle node}=[draw]                                                             % Just for debugging, not used!
    %\draw[fill,color=red] (0,0) rectangle (3.15cm,3.15cm);                                              % Just for debugging, not used!
    \ult@chapslashtikz                                                                                   % Draw UiT "slash"
    \draw[color=black] (1.1,1.18) node[rectangle,anchor=west] { \Huge\bfseries\chapnumfont\unhcopy\ult@chapnumbox\thechapter }; % Draw chapter number
  \end{tikzpicture}%
}

% Used to measure height of UiT slash
\newbox\ult@chapslashbox
\setbox\ult@chapslashbox\hbox{\begin{tikzpicture}\ult@chapslashtikz\end{tikzpicture}}
\newlength\ult@chapslash@height
\setlength{\ult@chapslash@height}{\ht\ult@chapslashbox}

\newcommand{\printrheadslash}[1][]{%
  \begingroup
    % This is a hotfix for a problem with using tikzpicture in running head (triggered when verbatim environment crosses a page boundary)
    % See issue #23 on GitHub. Hopefully, we'll find a better solution later.
    % Note: we only restore dollar catcode inside the local group.
    \catcode`\$=3
    %
    \raisebox{-0.6pt}{%
      \begin{tikzpicture}[scale=0.074]
        \ult@chapslashtikz % Draw UiT "slash"
      \end{tikzpicture}%
    }%
  \endgroup
}

% Align bottom of UiT slash with bottom of baseline
\newlength{\chapterheadalign}
\setlength{\chapterheadalign}{\z@ + 8\ult@normalbaselineskip}

% Align chapter name
\newlength{\chapternamealign}\setlength{\chapternamealign}{\z@}

% Extra vertical spacing that is added by book.cls in addition to the default 50\p@ (I think it is \vspace* that does it)
% See https://groups.google.com/forum/#!topic/comp.text.tex/tIoCvZWpF7U
\newlength{\chapterheadtopvspace}\setlength{\chapterheadtopvspace}{\parskip + \baselineskip}

%%%%%%%%%%%%%%% THESE CAN BE CHANGED!!!!! %%%%%%%%%%%%%%%
\def\baselinesbeforefrontchaphead{4} % Approximate same distance as would be with standard 50\p@ vspace % standard 8
\def\baselinesbeforemainchaphead{3} % Approximate same distance as would be with standard 50\p@ vspace % standard 6
\def\baselinesafterchapnum{2} % 3
\def\baselinesafterchaptitle{1}%2

\def\baselinesbeforesection{3}
\def\baselinesaftersection{1}

\def\baselinesbeforesubsection{2}
\def\baselinesaftersubsection{1}

\def\baselinesbeforesubsubsection{2}
\def\baselinesaftersubsubsection{1}
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

% Pretty size that spans two baselines. Height is approx. \baselineskip + \fontcharht\font`B
% baselineskip is set to 2.5 * \ult@normalbaselineskip (baselineskip of normal font), so that every second
% line aligns to the baseline of the normal font.
\def\chaptitlefontsize {\fontsize{26}{2.5\ult@normalbaselineskip}}

% \Large (of 10pt and 11pt) modified to skip 1.5 normal base lines, so that every second line aligns to
% baseline of the normal font.
\def\sectfontsize {\fontsize\@xivpt{1.5\ult@normalbaselineskip}}

% \large (of 10pt and 11pt) modified to skip exactly 1 base line
\def\subsectfontsize {\fontsize\@xiipt{\ult@normalbaselineskip}}

% Used to measure height of chapter name, section name, etc.
\newbox\ult@chaptername@vbox
\newdimen\ult@chapter@tmptotalheight
\newdimen\ult@chapter@tmpstrutheight
\newbox\ult@sectionname@vbox
\newdimen\ult@section@parwidth
\newdimen\ult@section@tmptotalheight
\newdimen\ult@section@tmpstrutheight

% Used for counting number of lines in chapter title, section title, etc.
\newcounter{ult@chapter@tmpnumlines}
\newcounter{ult@section@tmpnumlines}

% Override \rightskip for \RaggedRight. This could be adjusted.
% See ftp://ftp.rz.uni-wuerzburg.de/pub/tex/latex2e/contrib/ms/ragged2e.pdf
\RaggedRightRightskip\z@\@plus1 fil

% Now, use our supahdupah chapter style! YEEEEEEAAAAHHHHHH!
\titleformat{\chapter}[display]
  {% Format
    % Make sure that line spacing defined by \baselineskip will be preserved under all circumstances!
    % See http://tex.stackexchange.com/questions/49072/is-changing-lineskiplimit-to-some-negative-value-a-good-idea-and-what-the-valu
    % Note: this could be a potential source of future text formatting bugs (if lines are written on top of each other)
    \lineskiplimit=-\maxdimen
  }%
  {\vspace*{\baselinesbeforemainchaphead\ult@normalbaselineskip} \vspace*{-\baselinesbeforefrontchaphead\ult@normalbaselineskip} \vspace*{\chapterheadalign} \printchapternum}% Label
  {\z@ - \parskip + \chapternamealign + \baselinesafterchapnum\ult@normalbaselineskip}% Vertical distance between chapter number and chapter title
  {% Before-code plus chapter name (titlesec's explicit option is enabled, so #1 is chapter name)
    % Make sure Chapter title aligns to baseline
    %
    \chaptitlefont\chaptitlefontsize
    %
    % Prevent overfull hbox (make it OK to break line before entire line's hbox is filled).
    \microtypesetup{protrusion=false,spacing=false}%
    \RaggedRight
    %
    \selectfont
    %
    \vspace*{-\baselineskip}%
    %
    % Measure number of text lines in chapter name
    \setbox0=\vbox{\leavevmode #1\relax \kern \z@ \strut}%
    \ult@chapter@tmptotalheight=\ht0 \advance\ult@chapter@tmptotalheight by \dp0
    \setbox0=\vbox{Bq \kern \z@ \strut}%
    \ult@chapter@tmpstrutheight=\ht0 \advance\ult@chapter@tmpstrutheight by \dp0
    \setcounter{ult@chapter@tmpnumlines}{\DivideLengths{\ult@chapter@tmptotalheight}{\ult@chapter@tmpstrutheight}}%
    %
    % Print chapter name
    #1% Important not to add spaces behind chapter name
  }%
  [% After-code
    % Make sure that normal text following chapter title aligns to baseline
    %
    % The titlesec package inserts something like
    %  \kern \z@ \strut \@@par \nobreak
    % before this (see definition of \ttlh@display).
    %
    % Note: this only works as long as \lineskiplimit=-\maxdimen. Without that, the solution is infinitely more complex.
    % For the old solution, see e.g. commit be2341c37a93c0af6f665bd3d4a0b445c310e1d2
    %
    % Note: we have confirmed that this is needed (try changing value of \parskip right before \@@par).
    % It's probably \@@par that inserts a \parskip. Also note that \parskip does not change when changing font size...
    \vspace*{-\parskip}%
    %
    % Make sure total height + spacing corresponds to an even number of baselineskips for normal font.
    \ifodd\value{ult@chapter@tmpnumlines}\else
      \vspace*{0.5\ult@normalbaselineskip}% XXX: This value is hardcoded such that \baselineskip of \chaptitlefontsize + this value is an integer multiple of \ult@normalbaselineskip.
    \fi
    %
    % Now, switch to normal font
    \normalsize\normalfont\selectfont
    %
    % Strut the fuck up!
    \kern \z@ \strut
    %
    % Compensate for extra line added by strut
    \vspace*{-\parskip}%
    \vspace*{-\baselineskip}%
    %
    %
    %
    % If requested, reset gls expansion on each new chapter
    \ifult@resetallglsperchapter
      % Reset glossaries so every abbreviation will expand on first use (except those that have been unset)
      \ult@restoreglsstates
    \fi
  ]

% Adjust spacing around chapter title
\titlespacing*{\chapter}
  {\z@} % Left margin = 0
  {\z@ - \chapterheadtopvspace + \baselinesbeforefrontchaphead\ult@normalbaselineskip} % Vertical space before chapter head (50\p@ is standard from book.cls)
  {\z@ + \baselinesafterchaptitle\ult@normalbaselineskip} % Vertical space after title, before text (\ttl@chapafter is default)


% The titlesec package overrides definition of \section, but sectsty overrides that definition in turn.
% Re-define \section using titlesec's macros
% NOTE: We do this instead of renewing \section definition manually
% using \@startsection, because it allows a higher degree of flexibility.
\titleclass{\section}{straight}
\titleclass{\subsection}{straight}
\titleclass{\subsubsection}{straight}
\titleclass{\paragraph}{straight}

% Now, define how \section is formatted.
% This will define \ttlf@section as macro:#1-> \ttlh@hang{Format}{Label}{Sep}{Before-code}{After-code}
% (the last three arguments to \ttlh@hang come from \ttl@select{section}...).
\titleformat{\section}[hang]
  {% Format
    % Make sure that line spacing defined by \baselineskip will be preserved under all circumstances!
    % See http://tex.stackexchange.com/questions/49072/is-changing-lineskiplimit-to-some-negative-value-a-good-idea-and-what-the-valu
    % Note: this could be a potential source of future text formatting bugs (if lines are written on top of each other)
    \lineskiplimit=-\maxdimen
    %
    % This \null is needed to ensure space between consecutive chapter/section/subsection/... headers
    \null
    % And this is to compensate for \null after page break
    \if@nobreak\else
      \vspace*{-\parskip}%
    \fi
    %
    \sectfontsize \bfseries \sectfont
    %
    % Prevent overfull hbox (make it OK to break line before entire line's hbox is filled).
    \microtypesetup{protrusion=false,spacing=false}%
    \RaggedRight
    %
    \selectfont
    %
    \vspace*{-\baselineskip}%
  }%
  {\@seccntformat{section}}% Label
  {\z@}% Sep
  {% Before-code:
    % Measure number of text lines in section name
    \ult@section@parwidth=\textwidth
    % Note: \ttlh@hang uses \leftskip in titlesec versions < v.2.10.2, and \hangindent from v.2.10.2
    \advance\ult@section@parwidth by -\leftskip
    \advance\ult@section@parwidth by -\hangindent
    \setbox0=\vbox{\parbox{\ult@section@parwidth}{{#1}\kern \z@ \strut \@@par}}%
    \ult@section@tmptotalheight=\ht0 \advance\ult@section@tmptotalheight by \dp0
    \setbox0=\vbox{Bq \kern \z@ \strut}%
    \ult@section@tmpstrutheight=\ht0 \advance\ult@section@tmpstrutheight by \dp0
    \setcounter{ult@section@tmpnumlines}{\DivideLengths{\ult@section@tmptotalheight}{\ult@section@tmpstrutheight}}%
    %
    % Print section name (needed because we have enabled titlesec's explicit option)
    #1% Important not to add spaces behind section name
  }%
  [% After-code
    % Make sure that normal text following section title aligns to baseline
    %
    % The titlesec package inserts something like
    %  \kern \z@ \strut \@@par \nobreak
    % before this (see definition of \ttlh@hang).
    %
    % Note: this only works as long as \lineskiplimit=-\maxdimen. Without that, the solution is infinitely more complex.
    % For the old solution, see e.g. commit be2341c37a93c0af6f665bd3d4a0b445c310e1d2
    %
    % Note: we have confirmed that this is needed (try changing value of \parskip right before \@@par).
    % It's probably \@@par that inserts a \parskip. Also note that \parskip does not change when changing font size...
    \vspace*{-\parskip}%
    %
    % Make sure total height + spacing corresponds to an even number of baselineskips for normal font.
    \ifodd\value{ult@section@tmpnumlines}\else
      \vspace*{0.5\ult@normalbaselineskip}% XXX: This value is hardcoded such that \baselineskip of \sectfontsize + this value is an integer multiple of \ult@normalbaselineskip.
    \fi
    %
    % Now, switch to normal font
    \normalsize\normalfont\selectfont
    %
    % Add a strut, to make sure that following text is placed perfectly on baseline.
    % Note: we cannot use \kern\z@ before strut, because that will ruin the \nobreak that we want between a section header
    % and the following text (we do not want to have a section header at the end of a page, and the following text at the next page).
    \strut
    %
    % Compensate for extra line added by strut
    \vspace*{-\parskip}%
    \vspace*{-\baselineskip}%
  ]

% Starred version makes sure we suppress indentation of text following section title
\titlespacing*{\section}
  {\z@} % Left margin = 0
  {\z@ + \baselinesbeforesection\ult@normalbaselineskip} % Vertical space before
  {\z@ + \baselinesaftersection\ult@normalbaselineskip} % Vertical space after

% Now, define how \subsection is formatted.
\titleformat{\subsection}[hang]
  {% Format
    % This \null is needed to ensure space between consecutive chapter/section/subsection/... headers
    \null
    % And this is to compensate for \null after page break
    \if@nobreak\else
      \vspace*{-\parskip}%
    \fi
    %
    \subsectfontsize \bfseries \subsectfont
    %
    % Prevent overfull hbox (make it OK to break line before entire line's hbox is filled).
    \microtypesetup{protrusion=false,spacing=false}%
   % \RaggedRight
    %
    \selectfont
    %
    \vspace*{-\baselineskip}%
  }%
  {\@seccntformat{subsection}}% Label
  {\z@}% Sep
  {#1}% Before-code: print subsection name (needed because we have enabled titlesec's explicit option)
  []% After-code

% Starred version makes sure we suppress indentation of text following section title
\titlespacing*{\subsection}
  {\z@} % Left margin = 0
  {\z@ + \baselinesbeforesubsection\ult@normalbaselineskip} % Vertical space before
  {\z@ - \parskip + \baselinesaftersubsection\ult@normalbaselineskip} % Vertical space after

% Now, define how \subsection is formatted.
\titleformat{\subsubsection}[hang]
  {% Format
    % This \null is needed to ensure space between consecutive chapter/section/subsection/... headers
    \null
    % And this is to compensate for \null after page break
    \if@nobreak\else
      \vspace*{-\parskip}%
    \fi
    %
    \normalsize \bfseries \subsubsectfont
    %
    % Prevent overfull hbox (make it OK to break line before entire line's hbox is filled).
    \microtypesetup{protrusion=false,spacing=false}%
    \RaggedRight
    %
    \selectfont
    %
    \vspace*{-\baselineskip}%
  }%
  {\@seccntformat{subsubsection}}% Label
  {\z@}% Sep
  {#1}% Before-code: print subsubsection name (needed because we have enabled titlesec's explicit option)
  []% After-code

% Starred version makes sure we suppress indentation of text following section title
\titlespacing*{\subsubsection}
  {\z@} % Left margin = 0
  {\z@ + \baselinesbeforesubsubsection\ult@normalbaselineskip} % Vertical space before
  {\z@ - \parskip + \baselinesaftersubsubsection\ult@normalbaselineskip} % Vertical space after

% Now, define how \paragraph is formatted.
\titleformat{\paragraph}[runin]
  {% Format
    \null
    \vspace*{-\parskip}%
    \normalsize \bfseries \parafont \selectfont
    \vspace*{-\baselineskip}%
  }%
  {\@seccntformat{paragraph}}% Label
  {\z@}% Sep
  {#1}% Before-code: print paragraph name (needed because we have enabled titlesec's explicit option)
  []% After-code

%%% NOTE
%%% The \@startsection macro is defined as follows:
%%%  \@startsection{<name>}{<level>}{<indent>}{<beforeskip>}{<afterskip>}{<style>}*[<altheading>]{<heading>}
%%% where the part after the *, including the * is optional.
%%% See http://tex.stackexchange.com/questions/31780/where-can-i-find-help-files-or-documentation-for-commands-like-startsection-fo
%%% and http://www.tug.org/texlive/Contents/live/texmf-dist/doc/latex/base/source2e.pdf

% NOTE: chapter head is printed as:
%   #4{#8}\kern \z@ \strut \@@par \nobreak #5\@@par
% where #4 = before, #8 = title, #5 = after
% 'before' and 'after' are arguments from \titleformat
% (defined in \ttlh@display).

%%% From titlesec.sty:
%%% ----------------------------------
%%% The following tags are used:
%%% ttl@  : the generic tag used through the style
%%% ttlh@ : a shape definition
%%% ttlf@ : a macro containing the title format
%%% ttls@ : id. the title space
%%% ttlp@ : page key related macros
%%% ttll@ : level number
%%% ----------------------------------

% \ttl@mkchap has following arguments:
% 1: left        --- 1st \ttls@chapter arg == \z@
% 2: right       --- 2nd \ttls@chapter arg == \z@
% 3: before      --- 3rd \ttls@chapter arg == 2nd \titlespacing arg
% 4: after       --- 4th \ttls@chapter arg == 3rd \titlespacing arg
% 5: afterindent --- 5th \ttls@chapter arg == \@ne
% 6: name  == {chapter}
% 7: title

% \ttl@select has following arguments:
% 1: {chapter}   --- 6th \ttl@mkchap arg
% 2: left        --- 1st \ttl@mkchap arg == 1st \ttls@chapter arg == \z@
% 3: right       --- 2nd \ttl@mkchap arg == 2nd \ttls@chapter arg == \z@
% 4: title       --- 7th \ttl@mkchap arg

% Contains arguments from \titlespacing
%%% From titlesec.sty:
%%% -------------------------------------------
% % The surrounding space is stored in a macro
% % named \ttls@<section> whose content is
% % {left}{right}{before}{after}{afterindent}.
%%% -------------------------------------------
%%% 'before' is 2nd argument from \titlespacing, and 'after' is 3rd argument
%%% 'left' and 'right are zero (\z@), and 'afterindent' is \@ne
%\debugdef{ttls@chapter}

% \ttlh@<shape> defines the \titleformat shape
% We use the 'display' shape above to place title label in separate paragraph
% (see http://mirrors.ctan.org/macros/latex/contrib/titlesec/titlesec.pdf section 3.1)
%%% From titlesec.sty:
%%% -------------------------------------------
%%% % 1:global 2:label 3:sep 4:style 5:after 6:left 7:right 8:title
%%% \ttl@<shape> and \ttlh@<shape> take the following eight
%%% arguments:
%%% {format}{label}{sep}{before}{after}{left}{right}{title}
%%% where before and after refer to the format.
%%% With the option explicit, #4 contains the title and #8 is
%%% empty.
%%% -------------------------------------------
% Note: this macro contains the actual definition of what \@makechapterhead outputs!!
%\debugdef{ttlh@display}

% Macro for invoking \ttlh@display with arguments from \titleformat{\chapter}
%\debugdef{ttlf@chapter}


%:------------------------ Environments ----------------------

% Default environment names for abstract and acknowledgement.
% These needs to be defined, incase a language who does not define them is loaded (like samin)
% Therefore, if they are not defined, the compilation won't halt on an undefined control sequence within the classfile,
% but rather use the standard defined english term.
\newcommand{\abstractname}{Abstract}
\newcommand{\ackname}{Acknowledgements}

% Define ack and abstract for english and norwegian explicitly, even though they may be defined in the language pack.
\renamedefname[USenglish]{abstract}{Abstract}
\renamedefname[USenglish]{ack}{Acknowledgements}
\renamedefname[norsk]{abstract}{Sammendrag}
\renamedefname[norsk]{ack}{Takksigelser}

% Missing from samin babel package
\renamedefname[samin]{ack}{Giitosat}
\renamedefname[samin]{acronym}{Oanádusat}

% Defined incorrectly with capital first letter in babel's 'norsk'
\renamedefname[norsk]{see}{se}

% Control-ifs
\newif\ifult@hasacknowledgement\ult@hasacknowledgementfalse
\newif\ifult@hasabstract\ult@hasabstractfalse
\newif\ifult@hasdedication\ult@hasdedicationfalse
\newif\ifult@hasepigraph\ult@hasepigraphfalse

% Abstract
\newenvironment{abstract}{%
  \ifult@hasacknowledgement
    \ult@warning{'\abstractname' should precede '\ackname'.}%
  \fi
  \global\ult@hasabstracttrue
  \chapter{\abstractname}%
  \thispagestyle{empty}%
}{}

% Acknowledgements
\newenvironment{acknowledgement}{%
  \global\ult@hasacknowledgementtrue
  \chapter{\ackname}%
  \thispagestyle{empty}%
}{}

% Dedication
\newenvironment{dedication}{%
  \ifult@hasabstract
    \ult@warning{'Dedication' should precede '\abstractname'.}%
  \fi
  \ifult@hasacknowledgement
    \ult@warning{'Dedication' should precede '\ackname'.}%
  \fi
  \ifult@hasepigraph
    \ult@warning{'Dedication' should precede 'Epigraph'}%
  \fi
  \global\ult@hasdedicationtrue
  \cleardoublepage
  \phantomsection
  \chaptermark{}%
  \thispagestyle{empty}%
  \null
  \vfil
  \begingroup
  \center
  \fontsize{12pt}{14.4pt}\rmfamily\mdseries\itshape\selectfont
}{%
  \endcenter
  \endgroup
  \vfil
}

% Epigraph
\newcommand{\epigraphitem}[2]{%
  \ifdefined\@ult@epigraphenv
    \item{``#1\relax''\\{--}#2\relax}%
  \else
    \ult@critical@error{epigraph environment required for epigraphitem!}%
  \fi
}
\newenvironment{epigraph}{%
  \ifult@hasacknowledgement
    \ult@warning{'Epigraph' should precede '\ackname'.}%
  \fi
  \ifult@hasabstract
    \ult@warning{'Epigraph' should precede '\abstractname'.}%
  \fi
  \ifdefined\@ult@epigraphenv
    \ult@critical@error{Nested epigraphs are not allowed!}%
  \fi
  \global\ult@hasepigraphtrue
  \def\@ult@epigraphenv{}%
  %\clearpage\thispagestyle{empty}%
  \null\vfill
  \begin{flushright}%
}{%
  \end{flushright}%
  \vspace*{2\baselineskip}%
}


%:-------------------------- ToC lists -----------------------

% TODO:
% \l@part
% \l@chapter
% \l@section
% \l@subsection
% \l@subsubsection
% \l@paragraph
% \l@subparagraph
% \l@figure
% \l@table

% List of ToC lists (e.g. lof for List of Figures, lot for List of Tables, ...)
\newcommand*{\ult@toclists}{,}
\newcommand*{\ult@foralltoclists}[2]{%
  \@for#1:=\ult@toclists\do{\ifx#1\@empty\else#2\fi}%
}

\eappto\ult@toclists{lof,} % Append List of Figures to \ult@toclists
\eappto\ult@toclists{lot,} % Append List of Tables to \ult@toclists

\let\ult@save@l@part\l@part
\renewcommand*\l@part[2]{%
  \ifult@first@contentsline
    \vspace*{\ult@cftbeforechapskip}
    \vspace*{-\cftbeforepartskip}
  \fi
  \ult@save@l@part{#1}{#2}%
}

\newlength\ult@cftbeforepartskip
  \setlength{\ult@cftbeforepartskip}{2\ult@normalbaselineskip} % Default is 2.25em \@plus\p@
\newlength\ult@cftbeforechapskip
  \setlength{\ult@cftbeforechapskip}{\ult@normalbaselineskip} % Default is 1.0em \@plus\p@
\newlength\ult@cftbeforesecskip
  \setlength{\ult@cftbeforesecskip}{0pt} % Default is \z@ \@plus .2\p@

\newlength\ult@cftpartindent
  \setlength{\ult@cftpartindent}{0em} % Default is 0em
\newlength\ult@cftpartnumwidth
  \setlength{\ult@cftpartnumwidth}{0em} % Default is 0em

\newlength\ult@cftchapindent
  \setlength{\ult@cftchapindent}{0em} % Default is 0em
\newlength\ult@cftchapnumwidth
  \setlength{\ult@cftchapnumwidth}{1.5em} % Default is 1.5em

\newlength\ult@cftsecindent
  \setlength{\ult@cftsecindent}{1.5em} % Default is 1.5em
\newlength\ult@cftsecnumwidth
  \setlength{\ult@cftsecnumwidth}{2.3em} % Default is 2.3em

\newlength\ult@cftsubsecindent
  \setlength{\ult@cftsubsecindent}{3.8em} % Default is 3.8em
\newlength\ult@cftsubsecnumwidth
  \setlength{\ult@cftsubsecnumwidth}{3.2em} % Default is 3.2em

\newlength\ult@cftsubsubsecindent
  \setlength{\ult@cftsubsubsecindent}{7.0em} % Default is 7.0em
\newlength\ult@cftsubsubsecnumwidth
  \setlength{\ult@cftsubsubsecnumwidth}{4.1em} % Default is 4.1em

\newlength\ult@cftparaindent
  \setlength{\ult@cftparaindent}{10em} % Default is 10em
\newlength\ult@cftparanumwidth
  \setlength{\ult@cftparanumwidth}{5em} % Default is 5em

\newlength\ult@cftsubparaindent
  \setlength{\ult@cftsubparaindent}{12em} % Default is 12em
\newlength\ult@cftsubparanumwidth
  \setlength{\ult@cftsubparanumwidth}{6em} % Default is 6em

\newcommand{\ult@cftpartfont}     % part titles
           {\usefont{T1}{bch}{b}{n}\large}
\newcommand{\ult@cftchapfont}     % chapter titles
           {\usefont{T1}{bch}{b}{n}}
\newcommand{\ult@cftsecfont}      % section titles
           {\usefont{T1}{bch}{m}{n}}
\newcommand{\ult@cftpartpagefont} % part page numbers
           {\ult@cftpartfont}
\newcommand{\ult@cftchappagefont} % chapter page numbers
           {\ult@cftchapfont}
\newcommand{\ult@cftsecpagefont}  % section page numbers
           {\ult@cftsecfont}

% Skip
\setlength{\cftbeforepartskip}{\ult@cftbeforepartskip}
\setlength{\cftbeforechapskip}{\ult@cftbeforechapskip}
\setlength{\cftbeforesecskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforesubsecskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforesubsubsecskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforeparaskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforesubparaskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforefigskip}{\ult@cftbeforesecskip}
\setlength{\cftbeforetabskip}{\ult@cftbeforesecskip}

% Indent
\setlength{\cftpartindent}{\ult@cftpartindent}
\setlength{\cftchapindent}{\ult@cftchapindent}
\setlength{\cftsecindent}{\ult@cftsecindent}
\setlength{\cftsubsecindent}{\ult@cftsubsecindent}
\setlength{\cftsubsubsecindent}{\ult@cftsubsubsecindent}
\setlength{\cftparaindent}{\ult@cftparaindent}
\setlength{\cftsubparaindent}{\ult@cftsubparaindent}
\setlength{\cftfigindent}{\ult@cftsecindent}
\setlength{\cfttabindent}{\ult@cftsecindent}

% Num width
\setlength{\cftpartnumwidth}{\ult@cftpartnumwidth}
\setlength{\cftchapnumwidth}{\ult@cftchapnumwidth}
\setlength{\cftsecnumwidth}{\ult@cftsecnumwidth}
\setlength{\cftsubsecnumwidth}{\ult@cftsubsecnumwidth}
\setlength{\cftsubsubsecnumwidth}{\ult@cftsubsubsecnumwidth}
\setlength{\cftparanumwidth}{\ult@cftparanumwidth}
\setlength{\cftsubparanumwidth}{\ult@cftsubparanumwidth}
\setlength{\cftfignumwidth}{\ult@cftsecnumwidth}
\setlength{\cfttabnumwidth}{\ult@cftsecnumwidth}

% Fonts
\renewcommand{\cftpartfont}{\ult@cftpartfont}
\renewcommand{\cftchapfont}{\ult@cftchapfont}
\renewcommand{\cftsecfont}{\ult@cftsecfont}
\renewcommand{\cftsubsecfont}{\ult@cftsecfont}
\renewcommand{\cftsubsubsecfont}{\ult@cftsecfont}
\renewcommand{\cftparafont}{\ult@cftsecfont}
\renewcommand{\cftsubparafont}{\ult@cftsecfont}
\renewcommand{\cftfigfont}{\ult@cftsecfont}
\renewcommand{\cfttabfont}{\ult@cftsecfont}

% Page number fonts
\renewcommand{\cftpartpagefont}{\ult@cftpartpagefont}
\renewcommand{\cftchappagefont}{\ult@cftchappagefont}
\renewcommand{\cftsecpagefont}{\ult@cftsecpagefont}
\renewcommand{\cftsubsecpagefont}{\ult@cftsecpagefont}
\renewcommand{\cftsubsubsecpagefont}{\ult@cftsecpagefont}
\renewcommand{\cftparapagefont}{\ult@cftsecpagefont}
\renewcommand{\cftsubparapagefont}{\ult@cftsecpagefont}
\renewcommand{\cftfigpagefont}{\ult@cftsecpagefont}
\renewcommand{\cfttabpagefont}{\ult@cftsecpagefont}

% Override tocloft's \newlistentry
% First, allow \newlistentry (and hence, also \newlistof) command, even if entry has already been defined.
% This allows us to use \newlistof to generate list definition after e.g. defining a float using \newfloat.
% For example:
%        \newfloat{algorithm}{htbp}{loalgorithm}[chapter]
%        \newlistof{algorithm}{loalgorithm}{List of Algorithms}
%
\xpatchcmd{\newlistentry}
    {{\PackageError{tocloft}{#2 has been previously defined}{\@eha}}}
    {{}}
    {}
    {\ult@critical@error{Failed to patch \@backslashchar newlistentry}}
%
% Now, override the macro, so we may override default styling on new list entries.
\LetLtxMacro{\ult@savenewlistentry}{\newlistentry}
\renewcommand{\newlistentry}[4][\@empty]{%
  \ult@savenewlistentry[#1]{#2}{#3}{#4}%
  \setlength{\@nameuse{cftbefore#2skip}}{\ult@cftbeforesecskip}%
  \setlength{\@nameuse{cft#2indent}}{\ult@cftsecindent}%
  \setlength{\@nameuse{cft#2numwidth}}{\ult@cftsecnumwidth}%
  \@namedef{cft#2font}{\ult@cftsecfont}%
  \@namedef{cft#2pagefont}{\ult@cftsecpagefont}%
}

% Override tocloft's \newlistof
\LetLtxMacro{\ult@savenewlistof}{\newlistof}
\renewcommand{\newlistof}[4][\@empty]{%
  \ult@savenewlistof[#1]{#2}{#3}{#4}%
  %
  % Append list extension to \ult@toclists
  \eappto\ult@toclists{#3,}%
  %
  % Override \listof... macro to use tocbibind to generate list
  % (will also add list to ToC).
  \@namedef{listof#2}{%
    \begingroup
      \tocfile{#4}{#3}%
    \endgroup
  }%
}

% Override float package's \listof (for lists that do not use tocloft)
% See: http://tex.stackexchange.com/questions/58469/why-are-listof-and-listoffigures-styled-differently
\renewcommand*{\listof}[2]{%
  \@ifundefined{ext@#1}{\float@error{#1}}{%
    \expandafter\let\csname l@#1\endcsname \l@figure  % <- use layout of figure
    %
    % Append list extension to \ult@toclists
    % Note: this is safe even if it has already been added to \ult@toclists...
    \eappto\ult@toclists{\@nameuse{ext@#1},}%
    %
    % Use tocbibind to render list
    \begingroup
      \tocfile{#2}{\@nameuse{ext@#1}}%
    \endgroup
  }%
}

% Override newfloat package's \newfloat@list@of to use our \listof command,
% because it relies on the ability to first make customizations, and then typeset
% the list using \listoffigures, which won't work in this template.
\renewcommand*{\newfloat@list@of}[2]{%
  \listof{#1}{\@nameuse{list#1name}}%
}

% Used to determine if document contains figures, tables, listings, etc.
\usepackage[figure,table,lstlisting]{totalcount}

% List of figures
\let\ult@savelistoffigures\listoffigures
\renewcommand\listoffigures{%
  \iftotalfigures% Only output list if count > 0
    \cleardoublepage
    \ult@savelistoffigures
  \fi
}
% Make sure \listoffigures has effect at most once!
\ult@once{\listoffigures}

% List of tables
\let\ult@savelistoftables\listoftables
\renewcommand\listoftables{%
  \iftotaltables% Only output list if count > 0
    \cleardoublepage
    \ult@savelistoftables
  \fi
}
% Make sure \listoftables has effect at most once!
\ult@once{\listoftables}

% Register listings with tocloft
\begingroup
  \let\newcounter\@gobble
  \let\setcounter\@gobbletwo
  \globaldefs\@ne
  \let\c@loldepth\@ne
  \newlistof{listings}{lol}{\lstlistlistingname}
\endgroup
\let\l@lstlisting\l@listings

% List of Listings
\renamedefname[USenglish]{lstlistlisting}{List of Listings}
\renamedefname[norsk]{lstlistlisting}{Listinger}
\renewcommand\lstlistoflistings{%
  \iftotallstlistings% Only output list if count > 0
    \cleardoublepage
    % Use tocbibind to generate list of listings
    \listoflistings
  \fi
}
% Make sure \listoflistings has effect at most once!
\ult@once{\lstlistoflistings}

% List of Abbreviations
\renamedefname[USenglish]{acronym}{List of Abbreviations}
\renamedefname[norsk]{acronym}{Forkortelser}

 \usepackage{ult-printglossary}

% Prevent \printglossaries, since we have acronyms in frontmatter and glossary in back matter
\renewcommand*{\printglossaries}{%
  \ult@warning{Call to \@backslashchar printglossaries was ignored.}%
}


%:-------------------------- Book class overrides -----------------------

% List of glossary entries that has been unset or reset (and must be remembered as such...)
\newcommand*{\ult@glsstates}{,}
\let\ult@saveglsreset\glsreset
\let\ult@saveglsunset\glsunset

% Macros for remembering gls entry states (unset or reset)
\newcommand*{\ult@unsetgls}[1]{%
  \gls@ifnotmeasuring
  {%
    \glsdoifexists{#1}%
    {%
      \csname ifglo@#1@flag\endcsname% True means unset
        \global\eappto\ult@glsstates{-#1,}% Append gls entry to list (minus indicates that glsentry is unset)
      \fi
    }%
  }%
}
\newcommand*{\ult@rememberglsstates}[1][\@glo@types]{%
  \forallglsentries[#1]{\@glsentry}%
  {%
     \ult@unsetgls{\@glsentry}%
  }%
}
\newcommand*{\ult@preserveglsstates}{%
  \renewcommand*{\glsreset}[1]{%
    \gls@ifnotmeasuring
    {%
      \glsdoifexists{##1}%
      {%
        \global\eappto\ult@glsstates{+##1,}% Append gls entry to list (plus indicates that glsentry is reset)
      }%
    }%
  }%
  \renewcommand*{\glsunset}[1]{%
    \gls@ifnotmeasuring
    {%
      \glsdoifexists{##1}%
      {%
        \global\eappto\ult@glsstates{-##1,}% Append gls entry to list (minus indicates that glsentry is unset)
      }%
    }%
  }%
}
\newcommand*{\ult@restoreglsstates}{%
  % Restore \glsreset and \glsunset
  \let\glsreset\ult@saveglsreset
  \let\glsunset\ult@saveglsunset
  %
  % Reset all gls entries, so these will expand
  \glsresetall
  %
  % Re-apply overridden gls states
  \@for{\@glsentry}:=\ult@glsstates\do% Iterate over entries in \ult@glsstates
  {%
    \ifx\@glsentry\@empty% Ignore empty entries
    \else
      \StrLeft{\@glsentry}{1}[\firstchar]% Get leftmost character
      \StrGobbleLeft{\@glsentry}{1}[\@glsentry]% Remove leftmost character
      \IfStrEq{\firstchar}{+}{% If entry starts with a plus (indicates reset)
        \glsreset{\@glsentry}%
      }%
      {% If not
        \IfStrEq{\firstchar}{-}{% If entry start with a minus (indicates unset)
          \glsunset{\@glsentry}%
        }%
        {% If not
          \ult@critical@error{BUG in \@backslashchar ult@restoreglsstates}%
        }%
      }%
    \fi
  }%
}

% Frontmatter
\let\ult@savefrontmatter\frontmatter
\renewcommand\frontmatter{%
  \ult@savefrontmatter
  %
  % Clear running heads
  \markboth{}{}%
  %
  % Redefine what is in \chaptermark so that \leftmark contains chapter name. Thus "Abstract" and "Acknowledgements" will be written in header
  \renewcommand{\chaptermark}[1]{%
    % Note: we use two number signs (#) because we have renewcommand within renewcommand
    \markboth{\rheadfmt{##1}}% % \leftmark (will be written to the right on even/verso pages and to the left on odd/recto pages) is set to chapter title after start of new chapters
             {}%    % \rightmark is cleared after start of new chapters, and will be replaced after start of new section
    }%
  %
  % Set \leftmark to the left on odd pages
  % This will make sure that chapter title is written in front matter where section title is normally written in main matter,
  % that is, to the left on odd/recto pages (in addition to the right on even/verso pages, where it is already written)
  \fancyhead[LO]{\rheadfont\textcolor{rheadcolor}{\leftmark}}%
  %
  % Remember current gls states (if any gls entries have been unset)
  \ult@rememberglsstates
  %
  % Make sure that abbreviations are not expanded
  \glsunsetall
  %
  % Make sure that we retain future state modifications
  \ult@preserveglsstates
}

% Table of Contents
\renewcommand\tableofcontents{%
  % Modified version of original \tableofcontents (we use tocloft, and our running heads style),
  % so we do not invoke original \tableofcontents.
  % Note: we do *not* use tocbibind's \tocfile (and use tocbibind with nottoc option), since we
  % do not want to include ToC as entry in ToC
  \toc@start
  \chapter*{\contentsname}\prw@mkboth{\contentsname}%
  \@starttoc{toc}%
  \toc@finish
  %
  \renewcommand\tableofcontents{%
    \ult@critical@error{Duplicate \@backslashchar tableofcontents}%
  }%
  %
  % LISTS
  % 1: Figures/illustrations
  % 2: Tables
  % 3: Abbreviations
  % 4: Symbols
  % 5: Preface
  %
  \ifult@printlistoffigures
    \listoffigures
  \fi
  %
  \ifult@printlistoftables
    \listoftables
  \fi
}

% Mainmatter
\let\ult@savemainmatter\mainmatter
\renewcommand\mainmatter{%
  \ult@savemainmatter
  %
  % Clear running heads
  \markboth{}{}%
  %
  % Redefine \chaptermark, so it contains chapter number in front of chapter name (e.g. "2 My Chapter's Name")
  % No1e that we do not use \@chapapp (contains "chapter type"), because we do not want to write "Chapter" before the chapter number.
  \renewcommand{\chaptermark}[1]{%
    % Note: we use two number signs (#) because we have renewcommand within renewcommand
    \markboth{\rheadfmt{\@chapapp\ \thechapter}\hspace{5pt}\printrheadslash\hspace{5pt}\rheadfmt{##1}}% % \leftmark (will be written to the right on even/verso pages) is set to chapter title after start of new chapters
             {}%                 % \rightmark is cleared after start of new chapters, and will be replaced after start of new section
  }%
  %
  % Set odd pages to have \rightmark instead of \leftmark
  % This will make sure that section title is written to the left in running headers on odd (recto) pages
  \fancyhead[LO]{\rheadfont\textcolor{rheadcolor}{\rightmark}}%
  %
  % Reset glossaries so every abbreviation will expand on first use (except those that have been unset)
   \ult@restoreglsstates
}

% Backmatter
\let\ult@savebackmatter\backmatter
\renewcommand\backmatter{%
  \ult@savebackmatter
  %
  % Clear running heads
  \markboth{}{}%
  %
  % Redefine what is in \chaptermark so that \leftmark contains chapter name.
  \renewcommand{\chaptermark}[1]{%
    % Note: we use two number signs (#) because we have renewcommand within renewcommand
    \markboth{\rheadfmt{##1}}% % \leftmark (will be written to the right on even/verso pages and to the left on odd/recto pages) is set to chapter title after start of new chapters
             {}%    % \rightmark is cleared after start of new chapters, and will be replaced after start of new section
    }%
  %
  % Set \leftmark to the left on odd pages
  % This will make sure that chapter title is written in back matter where section title is normally written in main matter,
  % that is, to the left on odd/recto pages (in addition to the right on even/verso pages, where it is already written)
  \fancyhead[LO]{\rheadfont\textcolor{rheadcolor}{\leftmark}}%
}


%:-------------------------- Frontpage stuffs -----------------------

% Custom variables for the frontpage
\def\thesisfaculty#1{\gdef\@thesisfaculty{#1}}
\def\thesisprogramme#1{\gdef\@thesisprogramme{#1}}

\newif\ifult@frontpage@image@issingle
\ult@frontpage@image@issinglefalse % Default to false

\let\ult@frontpage@image@single\@empty
\def\ThesisFrontpageImage#1{%
  \ult@frontpage@image@issingletrue
  \gdef\ult@frontpage@image@single{#1}%
}

\let\ult@frontpage@image@left\@empty
\def\ThesisLeftFrontpageImage#1{%
  \ult@frontpage@image@issinglefalse
  \gdef\ult@frontpage@image@left{#1}%
}

\let\ult@frontpage@image@right\@empty
\def\ThesisRightFrontpageImage#1{%
  \ult@frontpage@image@issinglefalse
  \gdef\ult@frontpage@image@right{#1}%
}

\def\ult@frontpage@tikzpicture#1{%
  \begin{tikzpicture}[radius=1cm,delta angle=180]
    #1%
  \end{tikzpicture}
}

% Defines tikz picture for custom frontpage images
\def\ult@frontpage@customimage@tile#1{%
 % \path[fill overzoom image=#1]
    % Coordinates are in cm
    % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
    (0,0) -- (21.6,0) -- ++(0,19.3478) -- ++(-21.6,0) -- cycle;
}


\def\ult@frontpage@pattern@left{%
  \ult@frontpage@tikzpicture{%
    % Note: "tile" makes sure we effectively blit the image in place without zooming or translating
    %\path[fill tile image={sluttrapport_forside.pdf}]
  \path[fill tile image={ult_forside_helt_element.pdf}]
      % Coordinates are in cm
      %
      % Make sure coordinates are translated relative to image bottom left
      (0,1.5)
      % Make sure coordinates are scaled relative to image size (216mm x 303mm)
      (21.6,30.3)
      %
      % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
      (0,1.5) -- (13.434,1.5) -- (20.421,20.8478) -- (0,20.8478) -- cycle;
  }%
}

\def\ult@frontpage@pattern@right{%
  \ult@frontpage@tikzpicture{%
    % Note: "tile" makes sure we effectively blit the image in place without zooming or translating
        %\path[fill tile image={sluttrapport_forside.pdf}]
       \path[fill tile image={ult_forside_helt_element.pdf}]
      % Coordinates are in cm
      %
      % Make sure coordinates are translated relative to image bottom left
      (0,1.5)
      % Make sure coordinates are scaled relative to image size (216mm x 303mm)
      (21.6,30.3)
      %
      % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
      (13.469,1.5) -- (21.6,1.5) -- (21.6,20.8478) -- (20.52,20.8478) -- cycle;
  }%
}

\def\ult@frontpage@customimage@left{%
  \ult@frontpage@tikzpicture{%
    % Note: "tile" makes sure we effectively blit the image in place without zooming or translating
    \path[fill tile picture=\ult@frontpage@customimage@tile{\ult@frontpage@image@left}]
      % Coordinates are in cm
      %
      % Make sure coordinates are translated relative to image bottom left
      (0,0)
      %
      % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
      (0,0) -- (13.434,0) -- (20.421,19.3478) -- (0,19.3478) -- cycle;
  }%
}

\def\ult@frontpage@customimage@right{%
  \ult@frontpage@tikzpicture{%
    % Note: "tile" makes sure we effectively blit the image in place without zooming or translating
    \path[fill tile picture=\ult@frontpage@customimage@tile{\ult@frontpage@image@right}]
      % Coordinates are in cm
      %
      % Make sure coordinates are translated relative to image bottom left
      (0,0)
      %
      % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
      (13.469,0) -- (21.6,0) -- (21.6,19.3478) -- (20.52,19.3478) -- cycle;
  }%
}

\def\ult@frontpage@customimage@single{%
  \ult@frontpage@tikzpicture{%
    % Note: "tile" makes sure we effectively blit the image in place without zooming or translating
    \path[fill tile picture=\ult@frontpage@customimage@tile{\ult@frontpage@image@single}]
      % Coordinates are in cm
      % (Lower left) -- (Lower right) -- (Upper right) -- (Upper left)
      (0,0) -- (21.6,0) -- (21.6,19.3478) -- (0,19.3478) -- cycle;
  }%
}

% Error if \maketitle is used without defining \thesisfaculty{} and \thesisprogramme{}
\def\@thesisfaculty{\ult@critical@error{No \noexpand\thesisfaculty given}}
\def\@thesisprogramme{\ult@critical@error{No \noexpand\thesisprogramme given}}

% Use English as default
% Set the topleft frontpage logo to English
\gdef\ult@frontpage@logo{ult_logo_engelsk.pdf}

% norsk language - changes the frontpage logos and profile to the norwegian language
\iflanguage{norsk}{%
  % Set the topleft frontpage logo to Norwegian
  \gdef\ult@frontpage@logo{ult_logo_norsk.pdf}
}{}

% samin language - changes the frontpage logos and profile to the Sami language
% samin is not a standard language in babel - therefore, it yields the error "You haven't defined the language samin yet"
% if used like \iflanguage{samin}{...}{}. We thus switch on the \languagename macro provided by babel.
\def\languagetemp{samin}
\ifx\languagename\languagetemp
  % Set the topleft frontpage logo to Sami
  \gdef\ult@frontpage@logo{ult_logo_samisk.pdf}
\fi

\newif\ifult@has@titlepage
\ult@has@titlepagefalse % Default to false

\newif\ifult@titlepage@was@not@first@page
\ult@titlepage@was@not@first@pagefalse % Default to false

%%%%%%%%%%% COPYRIGHT PAGE %%%%%%%%%%%

\newbox\ult@copyright@page@box
\newbox\ult@tmp@shipoutbox

\fancypagestyle{ult-copyrightpage}{%
  \fancyhf{}%
  \fancyfoot[C]{%
    \color[cmyk]{0.3,0.3,0.3,0.9}%
    This thesis document was typeset using the \emph{UiT Thesis L\kern-.28em\raise.35ex\hbox{\textsc{a}}\kern-.12emT\kern-.12em\lower.38ex\hbox{E}\kern-.03emX Template}.\\
    \textcopyright{} \the\year{} -- \url{http://github.com/egraff/uit-thesis}%
  }%
}

\def\ult@copyright@page@contents{%
  \thispagestyle{ult-copyrightpage}%
  \null\vfill
}

\ifcsname tikzexternalrealjob\endcsname\else
  \ifult@copyright@disabled
    \ult@warning@nl{%
      You have disabled the copyright notice.\MessageBreak
      THIS MAY BE IN VIOLATION OF THE\MessageBreak
      LICENCE AGREEMENT!\MessageBreak
      See http://github.com/egraff/uit-thesis for more info%
    }%
    %
    \AfterBeginDocument{%
      \AtBeginShipoutNext{%
        \ifult@has@titlepage\else
          \global\ult@titlepage@was@not@first@pagetrue
        \fi
      }%
    }%
  \else
    % Copyright page
    \AfterBeginDocument{%
      \pagenumbering{alph}%
      \setcounter{page}{2}%
      %
      % Now, this may seem counter-intuitive (and it is!!), but this page label
      % will be associated with the first actual page to be shipped out!
      \renewcommand\thepage{A}%
      %
      % Record the following copyright page in \ult@copyrightpagebox
      \AtBeginShipoutNext{%
        \global\setbox\ult@copyright@page@box=\copy\AtBeginShipoutBox
        \AtBeginShipoutDiscard
      }%
      %
      % Start copyright page
      %
      \ult@copyright@page@contents
      \cleardoublepage
      %
      % End copyright page - setup first "normal" page
      %
      \pagenumbering{arabic}%
      \setcounter{page}{1}%
      %
      % Setup hook for first page
      \AtBeginShipoutNext{%
        \ifult@has@titlepage
          % First, shipout the title page
          % Note: we have to restore \protect first, which was overridden in \AtBegShi@Output.
          % Then, after shipout, we use LaTeX' \set@typeset@protect to change \protect the way
          % that was already done in \AtBegShi@Output.
          %
          \let\protect\noexpand
         % \ult@orig@shipout\box\AtBeginShipoutBox
          \set@typeset@protect
          %
          % Then, setup shipout of copyright page
          %
          %\setbox\AtBeginShipoutBox=\box\ult@copyright@page@box
          %
          % Make sure title page is displayed as recto page
          % (but only if using two-page layout)
          %
          \def\ult@tmp@pdfpagelayout{TwoPageLeft}%
          \ifx\@pdfpagelayout\ult@tmp@pdfpagelayout
            % We cannot use hyperref to change page layout, because we're after preamble.
            % So we use "direct" PDF command instead.
            \pdfcatalog{/PageLayout/TwoPageRight}%
          \fi
        \else
          % First page wasn't title page, so we insert copyright page before first page.
          \global\ult@titlepage@was@not@first@pagetrue
          %
          % First, save the current first page.
          \setbox\ult@tmp@shipoutbox=\copy\AtBeginShipoutBox
          %
          % Then, we shipout the copyright page.
          % Note: we have to restore \protect first, which was overridden in \AtBegShi@Output.
          % Then, after shipout, we use LaTeX' \set@typeset@protect to change \protect the way
          % that was already done in \AtBegShi@Output.
          %
          \let\protect\noexpand
          \ult@orig@shipout\box\ult@copyright@page@box
          \set@typeset@protect
          %
          % Restore original first page, so it will be shipped out too!
          %
          \setbox\AtBeginShipoutBox=\box\ult@tmp@shipoutbox
          %
          % Make sure copyright page is displayed as verso page
          % (but only if using two-page layout)
          %
          \def\ult@tmp@pdfpagelayout{TwoPageRight}%
          \ifx\@pdfpagelayout\ult@tmp@pdfpagelayout
            % We cannot use hyperref to change page layout, because we're after preamble.
            % So we use "direct" PDF command instead.
            \pdfcatalog{/PageLayout/TwoPageLeft}%
          \fi
        \fi
      }%
    }%
  \fi
\fi

%%%%%%%%%%% END COPYRIGHT PAGE %%%%%%%%%%%

\renewenvironment{titlepage}
  {%
    % If we have already shipped out first page, produce error!
    \ifult@titlepage@was@not@first@page
      \ClassError{uit-thesis}{Title page must be first page}\@ehd
    \fi
    %
    \ult@has@titlepagetrue
    % Fix PDF page numbering (title page must _not_ get page number 1, else we have two pages with that number)
    \pagenumbering{Alph}%
    \setcounter{page}{1}%
    %
    \ifult@copyright@disabled\else
      % Now, this may seem counter-intuitive (and it is!!), but this page label
      % will actually be associated with the copyright page!
      \renewcommand\thepage{B}%
    \fi
    %
    \cleardoublepage
    \newpage
    \thispagestyle{empty}%
    \setcounter{page}\@ne
  }%
  {%
    \newpage
    %
    \ifult@copyright@disabled\else
     % \clearpage
      \pagenumbering{Alph}%
      \setcounter{page}{3}%
    \fi
  }

\renewcommand\maketitle{%
  % Set custom geometry for the frontpage
  \newgeometry{%
    layout=a4paper,%
    layouthoffset=\ult@paper@hbleed,%
    layoutvoffset=\ult@paper@vbleed,%
    ignorehead,%
    ignorefoot,%
    top=\ult@frontpage@topmargin,%
    bottom=\ult@frontpage@bottommargin,%
    left=\ult@frontpage@leftmargin,%
    right=\ult@frontpage@rightmargin%
  }%
  %
  \begin{titlepage}%
    \hbox{}%
    %
    %\begin{egg}%
    \IfEndWith{\ult@author}{arlberq}{%
      \AddToShipoutPicture*{%
        \put(140,50){%
          \begingroup
            \printthed
          \endgroup
        }%
      }%
    }{%
    %\end{egg}
    }%
    %
    %%% Front page pattern / images
    \ifult@frontpage@image@issingle
      \AddToShipoutPicture*{%
        \AtPageUpperLeft{%
          \raisebox{-\ult@frontpage@graphics@ypos}{%
            \hspace*{-\ult@coverpages@graphics@hoffset}%
            \ult@frontpage@customimage@single
          }%
        }%
      }%
    \else
      %
      % Left front page image
      %
      \AddToShipoutPicture*{%
        \AtPageUpperLeft{%
          \raisebox{-\ult@frontpage@graphics@ypos}{%
          \hfill
            \hspace*{\ult@coverpages@graphics@hoffset}%
            \ifx\ult@frontpage@image@left\@empty
              \ult@frontpage@pattern@left
            \else
              \ult@frontpage@customimage@left
            \fi
          }%
        }%
      }%
      %
      % Right front page image
      %
      \AddToShipoutPicture*{%
        \AtPageUpperLeft{%
          \raisebox{-\ult@frontpage@graphics@ypos}{%
            \hspace*{\ult@coverpages@graphics@hoffset}%
            \ifx\ult@frontpage@image@right\@empty
              \ult@frontpage@pattern@right
            \else
              \ult@frontpage@customimage@right
            \fi
          }%
        }%
      }%
    \fi
    %
    % UiT Logo (Hugin & Munin)
    %
    \AddToShipoutPicture*{%
      \AtPageUpperLeft{%
        \raisebox{-\dimexpr\ult@coverpages@graphics@height-\ult@coverpages@graphics@voffset\relax}{%
          \hspace*{-\ult@coverpages@graphics@hoffset}%
          \includegraphics{\ult@frontpage@logo}%
        }%
      }%
    }%
    %
    \AddToShipoutPicture*{%
      \AtPageUpperLeft{%
        \raisebox{-\ult@frontpage@textbox@ypos}{%
          \hspace*{\ult@frontpage@textbox@xpos}%
          \begin{minipage}[b][0pt][b]{\textwidth}%
            %
            % Faculty / department
            %
            \begingroup
              \fontsize{12pt}{14.4pt}\usefont{T1}{\ult@opensans@TLF@family}{l}{n}%
              \noindent\@thesisfaculty
              \vskip -\parskip
              \vspace*{5pt}%
            \endgroup
            %
            % Thesis title
            %
            \begingroup
              \fontsize{14pt}{16.8pt}\usefont{T1}{\ult@opensans@TLF@family}{sb}{n}%
              \noindent\@title
              \vskip -\parskip
              \vspace*{5pt}%
            \endgroup
            %
            % Thesis subtitle
            %
            \begingroup
              \fontsize{12pt}{14.4pt}\usefont{T1}{\ult@opensans@TLF@family}{m}{n}%
              \noindent\@subtitle
              \vskip -\parskip
              \vspace*{14.4pt}%
            \endgroup
            %
            % Name of author
            %
            \begingroup
              \fontsize{10pt}{12pt}\usefont{T1}{\ult@opensans@TLF@family}{l}{n}%
              \noindent\@author
              \vskip -\parskip
              \vspace*{5pt}%
            \endgroup
            %
            % Thesis description one-line / study program / month and year
            %
            \begingroup
              \fontsize{10pt}{12pt}\usefont{T1}{\ult@opensans@TLF@family}{l}{n}%
              \noindent\@thesisprogramme
              \vskip -\parskip
            \endgroup
          \end{minipage}%
        }%
      }%
    }%
  \end{titlepage}%
  %
  % Restore original geometry
  \restoregeometry
  %
  \setcounter{footnote}{0}%
  \global\let\thanks\relax
  \global\let\maketitle\relax
  \global\let\@thanks\@empty
  \global\let\@author\@empty
  \global\let\@date\@empty
  \global\let\@title\@empty
  \global\let\@subtitle\@empty
  \global\let\title\relax
  \global\let\subtitle\relax
  \global\let\author\relax
  \global\let\date\relax
  \global\let\and\relax
  %
  % Add back page automatically
  %
  \AtEndDocument{%
    \cleardoublepage           % hvis man trenger en ekstra tom side....
    \thispagestyle{empty}%
    \hbox{}%
    %
    % Set custom geometry for the frontpage
    \newgeometry{%
      layout=a4paper,%
      layouthoffset=\ult@paper@hbleed,%
      layoutvoffset=\ult@paper@vbleed,%
      ignorehead,%
      ignorefoot,%
      top=\ult@frontpage@topmargin,%
      bottom=\ult@frontpage@bottommargin,%
      left=\ult@frontpage@leftmargin,%
      right=\ult@frontpage@rightmargin%
    }%
    %
    \newpage
    \thispagestyle{empty}%
    \hbox{}%
    %
    % Left back page image
    %
    \AddToShipoutPicture*{%
      \AtPageUpperLeft{%
        \raisebox{-\ult@frontpage@graphics@ypos}{%
          \hspace*{-\ult@coverpages@graphics@hoffset}%
          \ult@frontpage@pattern@left
        }%
      }%
    }%
    %
    % Right back page image
    %
    \AddToShipoutPicture*{%
      \AtPageUpperLeft{%
        \raisebox{-\ult@frontpage@graphics@ypos}{%
          \hspace*{-\ult@coverpages@graphics@hoffset}%
          \ult@frontpage@pattern@right
        }%
      }%
    }%
  }%
}


%:-------------------------- Convenient macros and definitions -----------------------

\def \const #1{\penalty 100 \hbox{\texttt{#1}}}

% todo macro
\newcommand{\todo}[1]{\textcolor{blue}{TODO:\\#1}}
% WIP macro
\newcommand{\wip}[1]{\textcolor{red}{W.I.P:\\#1}}

\let\ult@save@onefilewithoptions\@onefilewithoptions
\def\@onefilewithoptions#1[#2][#3]#4{%
  % Note: the \AtBeginDocument is set to \@firstofone in the \document macro.
  % It can therefore be used to test if command is used before or after \begin{document}...\end{document},
  % that is, if we're in the preamble or not. Since \@onefilewithoptions is defined with \@onlypreamble in
  % the LaTeX kernel, we don't want to take any action if we're not in the preamble, since it's an error anyways.
  \ifx\AtBeginDocument\@firstofone\else
    % We're in the preamble
    %
    % Test if the package is allowed
    \ifcsname ult@disallowpackage@#1\endcsname
      \begingroup
        \expandafter\let\expandafter\ult@tmp@disallowpackagereason\csname ult@eclareisallowpackage@#1\endcsname
        \ifx\ult@tmp@disallowpackagereason\@empty
          \ult@critical@error{The use of package #1 has been disallowed by the uit-thesis document class}%
        \else
          \ult@critical@error{The use of package #1 has been disallowed by the uit-thesis document class.^^J^^J\ult@tmp@disallowpackagereason^^J}%
        \fi
      \endgroup
    \else
      % Warn if package hasn't been previously loaded
      \@ifpackageloaded{#1}{}{\ult@warning@nl{Are you sure you are allowed to use the #1 package?}}%
    \fi
  \fi
  \ult@save@onefilewithoptions#1[{#2}][{#3}]#4%
}

% That's all, folks!
\endinput
