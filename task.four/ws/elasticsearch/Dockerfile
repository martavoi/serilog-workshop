FROM docker.elastic.co/elasticsearch/elasticsearch:6.2.4

ENV ES_JAVA_OPTS="-Xmx256m -Xms256m"

ADD elasticsearch.yml /usr/share/elasticsearch/config/
USER root
RUN chown elasticsearch:elasticsearch config/elasticsearch.yml
USER elasticsearch