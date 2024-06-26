book-text-atmos-distro =
    Распределительная сеть, или сокращенно "дистро", - это линия жизни станции. Она отвечает за транспортировку воздуха из атмосферной системы по всей станции.
    
    Соответствующие трубы часто окрашены в приглушенный синий цвет, но верный способ определить их - использовать Х-лучевой сканер, чтобы отследить, какие трубы подключены к активным вентиляционным отверстиям на станции.
    
    Стандартная газовая смесь распределительной сети составляет 20 градусов Цельсия, 78% азота, 22% кислорода. Вы можете проверить это с помощью газоанализатора на распределительной трубе или любом соединенном с ней вентиляционном отверстии. В особых обстоятельствах могут потребоваться специальные смеси.
    
    При выборе давления в распределителе необходимо учитывать несколько моментов. Активные вентиляционные отверстия регулируют давление на станции, поэтому, пока все работает правильно, слишком высокое давление не бывает.
    
    Более высокое давление в распределительной сети позволит распределительной сети действовать в качестве буфера между газовыми добытчиками и вентиляционными отверстиями, обеспечивая значительное количество дополнительного воздуха, который может быть использован для повторного нагнетания давления на станции после разгерметизации.
    
    Более низкое давление дистро уменьшит количество теряемого газа в случае разрыва дистро, что является быстрым способом борьбы с загрязнением дистро. Это также может помочь замедлить или предотвратить избыточное давление на станции в случае проблем с вентиляцией.
    
    Обычное давление дистро находится в диапазоне 300-375 кПа, но можно использовать и другие давления, зная о рисках и преимуществах.
    
    Давление в сети определяется последним насосом, закачивающим в нее Для предотвращения образования узких мест все остальные насосы между добытчиками и последним насосом должны быть установлены на максимальную скорость, а все ненужные устройства должны быть удалены.
    
    Вы можете проверить давление в дистро с помощью газоанализатора, но имейте в виду, что высокое количество таких вещей, как разгерметизации, может привести к тому, что давление в дистро будет ниже установленного целевого давления в течение длительного времени. Поэтому, если Вы видите падение давления, не паникуйте - оно может быть временным.
book-text-atmos-waste =
    Сеть удаления отходов является основной системой, отвечающей за поддержание воздуха на станции свободным от загрязняющих веществ.
    
    Вы можете определить соответствующие трубы по их приятному тускло-красному цвету или с помощью Х-лучевого сканера отследить, какие трубы подключены к скрубберам на станции.
    
    Сеть отходов используется для транспортировки отходящих газов для фильтрации или разделения. Идеальным является поддержание давления на уровне 0 кПа, но в процессе эксплуатации иногда может возникать ненулевое давление.
    
    У атмос-техников есть возможность фильтровать или отводить отработанные газы. В то время как разделение происходит быстрее, фильтрация позволяет повторно использовать газы для переработки или продажи.
    
    Сеть отходов также может быть использована для диагностики атмосферных проблем на станции. Высокие уровни отходящих газов могут свидетельствовать о крупной утечке, в то время как присутствие не отходящих газов может указывать на конфигурацию скруббера или проблемы с физическим соединением. Если газы имеют высокую температуру, это может свидетельствовать о пожаре.
book-text-atmos-alarms =
    Воздушные сигнализации расположены на всех станциях, что позволяет управлять и контролировать местную атмосферу.
    
    Интерфейс воздушной сигнализации предоставляет техническому персоналу список подключенных датчиков, их показания и возможность настройки пороговых значений. Эти пороговые значения используются для определения состояния тревоги воздушной сигнализации. Техники также могут использовать интерфейс для установки целевых давлений для вентиляционных отверстий и настройки рабочих скоростей и целевых газов для скрубберов.
    
    Хотя интерфейс позволяет выполнять тонкую настройку устройств, находящихся под управлением воздушной сигнализации, имеется также несколько режимов для быстрой конфигурации сигнализации. Эти режимы автоматически переключаются при изменении состояния сигнализации:
    - Фильтрация: Режим по умолчанию
    - Фильтрация (широкая): Режим фильтрации, который изменяет работу скрубберов для очистки более широкой области.
    - Заполнение: отключает скрубберы и устанавливает вентиляционные отверстия на максимальное давление.
    - Паника: Отключает вентиляционные отверстия и устанавливает скрубберы на сифон.
    
    Для соединения устройств с воздушной сигнализацией используется мультитул.
book-text-atmos-vents =
    Ниже приводится краткое справочное руководство по нескольким атмосферным устройствам:
    
                Пассивные вентиляционные устройства:
                Эти вентиляционные устройства не требуют питания, они позволяют газам свободно течь как в сеть труб, к которой они присоединены, так и из нее.
    
                Активные вентиляционные отверстия:
                Это самые распространенные вентиляционные отверстия на станции. Они оснащены внутренним насосом и требуют питания. По умолчанию они выкачивают газы только из труб, и только до 101 кпа. Однако их можно перенастроить с помощью воздушной сигнализации. Они также блокируются, если в помещении давление ниже 1 кпа, чтобы предотвратить откачку газов в космос.
    
                Скрубберы воздуха:
                Эти устройства позволяют удалять газы из окружающей среды и вводить их в подключенную сеть труб. Они могут быть настроены на отбор определенных газов при подключении к воздушной сигнализации.
    
                Инжекторы воздуха:
                Инжекторы похожи на активные воздухоотводчики, но они не имеют внутреннего насоса и не требуют питания. Их нельзя настроить, но они могут продолжать откачивать газы до гораздо более высоких давлений.
